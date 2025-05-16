using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiteOrderWeb.Data;
using BiteOrderWeb.Models;
using System.Linq;
using System.Threading.Tasks;
using BiteOrderWeb.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BiteOrderWeb.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly AppDbContext _context;

        public DriverController(UserManager<Users> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Home()
        {
            var user = await _userManager.GetUserAsync(User);

            var acceptedOrders = await _context.Orders
                .Where(o => o.DriverId == user.Id && o.Status == Order.OrderStatus.InProgress)
                .ToListAsync();

            var pendingOrders = await _context.Orders
                .Where(o => o.Status == Order.OrderStatus.Pending && o.DriverId == null && o.Address.Area.Name == user.DeliveryArea)
                .ToListAsync();

            var lastOrder = acceptedOrders.OrderByDescending(o => o.Date).FirstOrDefault();

            var vm = new DriverHomeViewModel
            {
                FullName = user.FullName,
                AcceptedOrdersCount = acceptedOrders.Count,
                PendingOrdersCount = pendingOrders.Count,
                LastOrderTime = lastOrder?.Date
            };

            return View(vm);
        }




        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null || string.IsNullOrWhiteSpace(driver.DeliveryArea))
                return NotFound("Driver or delivery area not found.");

            var rejections = await _context.OrderRejections
                .Where(r => r.DriverId == driver.Id)
                .ToListAsync();

           
            var allOrders = await _context.Orders
                .Include(o => o.Address)
                    .ThenInclude(a => a.Area)
                .Include(o => o.User)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                    .ThenInclude(p => p.RestaurantProducts)
                    .ThenInclude(rp => rp.Restaurant)
                .Where(o => o.DriverId == null
                    && o.Status == Order.OrderStatus.Pending
                    && o.Address != null
                    && o.Address.Area != null
                    && o.Address.Area.Name.ToLower() == driver.DeliveryArea.ToLower())
                .ToListAsync();

            
            var filteredOrders = allOrders
                .Where(o =>
                    !rejections.Any(r => r.OrderId == o.Id) ||
                    rejections.Any(r => r.OrderId == o.Id && r.ShowAgain)
                )
                .ToList();

            return View(filteredOrders);
        }



        [HttpPost]
        public async Task<IActionResult> MarkAsDone(int orderId)
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null)
                return NotFound();

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.DriverId != driver.Id)
                return BadRequest("Invalid order.");

            order.Status = Order.OrderStatus.Done;
            order.DeliveredAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Order marked as done.";

            return RedirectToAction("AcceptedOrders");
        }


        [HttpGet]
        public async Task<IActionResult> AcceptedOrders()
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null)
                return NotFound();

            var orders = await _context.Orders
                .Include(o => o.Address).ThenInclude(a => a.Area)
                .Include(o => o.User)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                        .ThenInclude(p => p.RestaurantProducts)
                            .ThenInclude(rp => rp.Restaurant)
                .Where(o => o.DriverId == driver.Id && o.Status == Order.OrderStatus.InProgress)
                .ToListAsync();

            return View(orders);
        }



        [HttpPost]
        public async Task<IActionResult> AcceptOrder(int orderId)
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null)
                return NotFound();

            var order = await _context.Orders
                .Include(o => o.Address).ThenInclude(a => a.Area)
                .Include(o => o.User)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Product)
                        .ThenInclude(p => p.RestaurantProducts)
                            .ThenInclude(rp => rp.Restaurant)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            if (order.DriverId != null || order.Status != Order.OrderStatus.Pending)
                return BadRequest("Order already accepted.");
            var driverId = _userManager.GetUserId(User);
            var fullDriver = await _userManager.FindByIdAsync(driverId);
            order.DriverId = driver.Id;
            order.Status = Order.OrderStatus.InProgress;
            order.AcceptedAt = DateTime.Now;
            order.AcceptedDriverName = fullDriver.FullName;
            var previousRejections = _context.OrderRejections.Where(r => r.OrderId == order.Id);
            _context.OrderRejections.RemoveRange(previousRejections);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Order accepted successfully!";
            return RedirectToAction("Orders");
        }


        [HttpPost]
        public async Task<IActionResult> RejectOrder(int orderId)
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null) return Unauthorized();

        
            bool alreadyRejected = await _context.OrderRejections
                .AnyAsync(r => r.OrderId == orderId && r.DriverId == driver.Id);

            if (!alreadyRejected)
            {
                var rejection = new OrderRejection
                {
                    OrderId = orderId,
                    DriverId = driver.Id,
                    RejectedAt = DateTime.UtcNow
                };

                _context.OrderRejections.Add(rejection);
                await _context.SaveChangesAsync();
            }
            TempData["Error"] = "Order rejected successfully.";

            return RedirectToAction("Orders");
        }



        [HttpGet]
        public async Task<IActionResult> Dashboard(bool edit = false)
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null) return NotFound();

            var areas = await _context.Areas
               .Where(a => !a.IsDeleted) 
                 .ToListAsync();
            var acceptedOrders = await _context.Orders
     .CountAsync(o => o.DriverId == driver.Id &&
         (o.Status == Order.OrderStatus.InProgress || o.Status == Order.OrderStatus.Done));


            var rejectedOrders = await _context.OrderRejections
                .CountAsync(r => r.DriverId == driver.Id);

            var model = new DriverProfileViewModel
            {
                Id = driver.Id,
                FullName = driver.FullName,
                PhoneNumber = driver.PhoneNumber,
                DeliveryArea = driver.DeliveryArea,
                ImageUrl = driver.ProfilePictureUrl,
                AcceptedOrdersCount = acceptedOrders,
                RejectedOrdersCount = rejectedOrders,
                Areas = areas
                .Select(a => new SelectListItem
                {
                    Value = a.Name,
                    Text = a.Name
                }).ToList()
            };

            ViewData["IsEdit"] = edit;
            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> Dashboard(DriverProfileViewModel model)
        {
            var driver = await _userManager.FindByIdAsync(model.Id);
            if (driver == null) return NotFound();
            if (!string.IsNullOrEmpty(model.CurrentPassword))
            {
                var passwordCheck = await _userManager.CheckPasswordAsync(driver, model.CurrentPassword);
                if (!passwordCheck)
                {
                    ModelState.AddModelError("", "Current password is incorrect.");
                    return View(model); 
                }
            }
            driver.FullName = model.FullName;
            driver.PhoneNumber = model.PhoneNumber;
            driver.DeliveryArea = model.DeliveryArea; 

         
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                var fileName = $"{Guid.NewGuid()}_{model.ImageFile.FileName}";
                var path = Path.Combine(uploads, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }

                driver.ProfilePictureUrl = "/uploads/" + fileName;
            }

            
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(driver);
                var passwordResult = await _userManager.ResetPasswordAsync(driver, token, model.NewPassword);

                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                        ModelState.AddModelError("", error.Description);

                    
                    model.Areas = await _context.Areas
                        .Where(a => !a.IsDeleted)
                        .Select(a => new SelectListItem { Value = a.Name, Text = a.Name })
                        .ToListAsync();

                    return View(model);
                }
            }

            var result = await _userManager.UpdateAsync(driver);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Failed to update profile.";

               
                model.Areas = await _context.Areas
                    .Select(a => new SelectListItem { Value = a.Name, Text = a.Name })
                    .ToListAsync();

                return View(model);
            }

            TempData["Success"] = "Profile updated!";
            return RedirectToAction("Dashboard");
        }





    }
}
