using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiteOrderWeb.Data;
using BiteOrderWeb.Models;
using System.Threading.Tasks;
using BiteOrderWeb.ViewModels;
using System.Drawing;
using Microsoft.AspNetCore.Mvc.Rendering;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<Users> _userManager;
    private readonly AppDbContext _context;

    public AdminController(UserManager<Users> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }
    public IActionResult Home()
    {
        return View(); 
    }
    public async Task<IActionResult> Dashboard()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var restaurant = await _context.Restaurants
            .Include(r => r.Address)
                .ThenInclude(a => a.Area) 
            .Include(r => r.RestaurantProducts)
                .ThenInclude(rp => rp.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AdminId == user.Id);

        if (restaurant == null) return NotFound("No restaurant assigned to this Admin");

        var model = new AdminDashboardViewModel
        {
            RestaurantName = restaurant.Name,
            AreaName = restaurant.Address?.Area?.Name ?? "N/A", 

            Street = restaurant.Address?.Street ?? "N/A",
            Building = restaurant.Address?.Building ?? "N/A",
            Floor = restaurant.Address?.Floor ?? "N/A",
            PhoneNumber = restaurant.Phone,
            ImageUrl = restaurant.ImageUrl,
            OpeningTime = restaurant.OpeningTime,
            ClosingTime = restaurant.ClosingTime,
            ProductCount = restaurant.RestaurantProducts.Count,
            TotalOrders = await _context.Orders
    .Include(o => o.OrderProducts)
        .ThenInclude(op => op.Product)
            .ThenInclude(p => p.RestaurantProducts)
    .CountAsync(o => o.OrderProducts.Any(op =>
        op.Product.RestaurantProducts.Any(rp => rp.RestaurantId == restaurant.Id)))



        };

        return View(model);
    }


    [HttpPost]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.OrderProducts)  
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

       
        var ordersWithProduct = await _context.Orders
            .Where(o => o.OrderProducts.Any(op => op.ProductId == id))
            .ToListAsync();

        if (ordersWithProduct.Any())
        {
            
            TempData["ErrorMessage"] = "you can not delete this product now, because there are orders related to it.";
            return RedirectToAction("ProductList");  
        }

      
        product.IsActive = false;
        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "this product is hiding now!";
        return RedirectToAction("ProductList"); 
    }





    [HttpGet]
    public async Task<IActionResult> EditRestaurant()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var restaurant = await _context.Restaurants
            .Include(r => r.Address)
            .FirstOrDefaultAsync(r => r.AdminId == user.Id);

        if (restaurant == null) return NotFound();

        var model = new EditRestaurantViewModel
        {
            RestaurantId = restaurant.Id,
            RestaurantName = restaurant.Name,
            PhoneNumber = restaurant.Phone,
            AreaId = restaurant.Address?.AreaId ?? 0,
            Street = restaurant.Address?.Street ?? "",
            Building = restaurant.Address?.Building,
            Floor = restaurant.Address?.Floor ?? "",
            ProfilePictureUrl = restaurant.ImageUrl,
            OpeningTime = restaurant.OpeningTime,
            ClosingTime = restaurant.ClosingTime,
            Areas = await _context.Areas
            .Where(a => !a.IsDeleted)
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Name
            }).ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditRestaurant(EditRestaurantViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Areas = await _context.Areas.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Name
            }).ToListAsync();

            return View(model);
        }

        var restaurant = await _context.Restaurants
            .Include(r => r.Address)
            .FirstOrDefaultAsync(r => r.Id == model.RestaurantId);

        if (restaurant == null) return NotFound();

        restaurant.Name = model.RestaurantName;
        restaurant.Phone = model.PhoneNumber;
        restaurant.OpeningTime = model.OpeningTime;
        restaurant.ClosingTime = model.ClosingTime;

        if (restaurant.Address == null)
        {
            var address = new Address
            {
                Street = model.Street,
                Building = string.IsNullOrWhiteSpace(model.Building) ? "N/A" : model.Building,
                Floor = string.IsNullOrWhiteSpace(model.Floor) ? "N/A" : model.Floor,
                AreaId = model.AreaId
            };

            _context.Add(address);
            await _context.SaveChangesAsync();

            restaurant.AddressId = address.Id;
        }
        else
        {
            restaurant.Address.Street = model.Street;
            restaurant.Address.Building = model.Building;
            restaurant.Address.Floor = model.Floor;
            restaurant.Address.AreaId = model.AreaId;
        }

        var admin = await _userManager.FindByIdAsync(restaurant.AdminId);
        if (admin != null)
        {
            admin.PhoneNumber = model.PhoneNumber;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                Directory.CreateDirectory(uploads);

                var fileName = $"{Guid.NewGuid()}_{model.ImageFile.FileName}";
                var filePath = Path.Combine(uploads, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.ImageFile.CopyToAsync(stream);

                restaurant.ImageUrl = "/uploads/" + fileName;
                admin.ProfilePictureUrl = restaurant.ImageUrl;
            }
            else if (!string.IsNullOrEmpty(model.ProfilePictureUrl))
            {
                restaurant.ImageUrl = model.ProfilePictureUrl;
                admin.ProfilePictureUrl = model.ProfilePictureUrl;
            }

            await _userManager.UpdateAsync(admin);
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Restaurant updated successfully!";
        return RedirectToAction("Dashboard");
    }


    [HttpGet]
    public async Task<IActionResult> RestaurantOrders(DateTime? date, string category)
    {
        var user = await _userManager.GetUserAsync(User);
        var restaurantId = await _context.Restaurants
            .Where(r => r.AdminId == user.Id)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var query = _context.Orders
            .Where(o => o.OrderProducts
                .Any(op => op.Product.RestaurantProducts
                    .Any(rp => rp.RestaurantId == restaurantId)))
            .Include(o => o.User)
            .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
            .AsQueryable();

        if (date.HasValue)
            query = query.Where(o => o.Date.Date == date.Value.Date);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(o => o.OrderProducts.Any(op => op.Product.Category.Contains(category)));

        var orders = await query.Select(o => new RestaurantOrderSummaryViewModel
        {
            OrderId = o.Id,
            CustomerName = o.User.FullName,
            Date = o.Date.ToShortDateString(),
            Time = o.Date.ToString("h:mm tt"),
            Categories = o.OrderProducts
        .Select(op => op.Product.Category)
        .Distinct()
        .ToList(),
            Products = o.OrderProducts.Select(op => op.Product.Name).ToList(),
            Quantities = o.OrderProducts
        .Select(op => op.Quantity)
        .ToList()
        }).ToListAsync();

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> TopOrderedDishes()
    {
        var user = await _userManager.GetUserAsync(User);

        var restaurantId = await _context.Restaurants
            .Where(r => r.AdminId == user.Id)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var dishOrders = await _context.OrderProduct
            .Where(op => op.Product.RestaurantProducts.Any(rp => rp.RestaurantId == restaurantId))
            .GroupBy(op => new { op.Product.Name, op.Product.Category })
            .Select(group => new
            {
                DishName = group.Key.Name,
                Category = group.Key.Category,
                TotalOrders = group.Sum(op => op.Quantity)
            })
            .ToListAsync();

        var total = dishOrders.Sum(d => d.TotalOrders);

        var topDishes = dishOrders
            .Select(d => new TopOrderedDishViewModel
            {
                DishName = d.DishName,
                Category = d.Category,
                TotalOrders = d.TotalOrders,
                Percentage = total == 0 ? 0 : Math.Round((double)d.TotalOrders / total * 100, 1)
            })
            .OrderByDescending(d => d.TotalOrders)
            .ToList();

        return View(topDishes);
    }


    [HttpGet]
    public async Task<IActionResult> OrdersPerDayChart(DateTime? fromDate, DateTime? toDate)
    {
        var user = await _userManager.GetUserAsync(User);

        var restaurantId = await _context.Restaurants
            .Where(r => r.AdminId == user.Id)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var ordersQuery = _context.Orders
            .Where(o => o.OrderProducts
                .Any(op => op.Product.RestaurantProducts
                    .Any(rp => rp.RestaurantId == restaurantId)));

        if (fromDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.Date >= fromDate.Value);

        if (toDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.Date <= toDate.Value);

        var ordersPerDay = await ordersQuery
            .GroupBy(o => o.Date.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(g => g.Date)
            .ToListAsync();

        var model = new OrdersTrendFilterViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            OrdersPerDay = ordersPerDay.Select(o => new OrdersPerDayViewModel
            {
                Date = o.Date,
                TotalOrders = o.Count
            }).ToList()
        };
        
        return View(model);
    }




}

