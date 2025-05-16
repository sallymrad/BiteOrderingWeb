using BiteOrderWeb.Data;
using BiteOrderWeb.Models;
using BiteOrderWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.IO;
using static BiteOrderWeb.Models.Order;



namespace BiteOrderWeb.Controllers
{

    [Authorize(Roles = "SuperAdmin")]

    public class SuperAdminController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<SuperAdminController> _logger;
        private readonly AppDbContext _context;
        private readonly SignInManager<Users> _signInManager;
        public SuperAdminController(SignInManager<Users> signInManager, UserManager<Users> userManager, RoleManager<IdentityRole> roleManager, ILogger<SuperAdminController> logger, AppDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult SuperAdminHome()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ManageRestaurants()
        {
            var restaurants = await _context.Restaurants.Include(r => r.AdminUser)
                 .AsNoTracking()
                .ToListAsync();
            return View(restaurants);
        }


        [HttpGet]
        public IActionResult AddRestaurant()
        {
            var model = new RestaurantViewModel();
            return View(model);
        }


        [HttpGet("restaurants")]
        public async Task<IActionResult> GetRestaurants()
        {
            var restaurants = await _context.Restaurants.Include(r => r.AdminUser).ToListAsync();
            return Ok(restaurants);
        }
        [HttpPost]
        public async Task<IActionResult> AddRestaurant(RestaurantViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);


            var existingAdmin = await _userManager.FindByEmailAsync(model.AdminEmail);
            if (existingAdmin != null)
            {
                ModelState.AddModelError("AdminEmail", "An admin with this email already exists.");
                return View(model);
            }


            var adminUser = new Users
            {
                FullName = "Admin " + model.Name,
                Email = model.AdminEmail,
                UserName = model.AdminEmail,
                PhoneNumber = model.Phone,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(adminUser, model.AdminPassword);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Failed to create Admin.");
                return View(model);
            }

            await _userManager.AddToRoleAsync(adminUser, "Admin");


            var restaurant = new Restaurant
            {
                Name = model.Name,
                Description = model.Description,
                Phone = model.Phone,
                AdminId = adminUser.Id
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageRestaurants");
        }




      



        [HttpGet]
        public async Task<IActionResult> AddDriver()
        {
            var allAreas = await _context.Areas
                .Where(a => !a.IsDeleted)
                .ToListAsync();

            var areas = allAreas
                .GroupBy(a => a.Name.ToLower())
                .Select(g => g.First())
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.Name
                })
                .ToList();

            var model = new AddDriverViewModel
            {
                Areas = areas
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddDriver(AddDriverViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Areas = await GetDistinctAreasAsync();
                return View(model);
            }


            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email is already used.");
                model.Areas = await GetDistinctAreasAsync();
                return View(model);
            }

            var selectedArea = await _context.Areas.FindAsync(model.AreaId);

            var driverUser = new Users
            {
                FullName = model.FullName,
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true,
                DeliveryArea = selectedArea?.Name
            };

            var result = await _userManager.CreateAsync(driverUser, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                model.Areas = await GetDistinctAreasAsync();
                return View(model);
            }

            await _userManager.AddToRoleAsync(driverUser, "Driver");

            return RedirectToAction("ManageDrivers");
        }



       



        private async Task<List<SelectListItem>> GetDistinctAreasAsync()
        {
            var allAreas = await _context.Areas
                .Where(a => !a.IsDeleted)
                .ToListAsync();

            return allAreas
                .GroupBy(a => a.Name.ToLower())
                .Select(g => g.First())
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.Name
                })
                .ToList();
        }




        public async Task<IActionResult> ManageUsers(string role)
        {
            var rolePriority = new List<string> { "SuperAdmin", "Admin", "Driver", "Client" };
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new Dictionary<string, string>();
            var orders = await _context.Orders.ToListAsync(); 

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.Any() ? roles.First() : "No Role";
                userRoles[user.Id] = userRole;

              
                user.HasOrders = orders.Any(o => o.UserId == user.Id);
            }

            if (!string.IsNullOrEmpty(role))
            {
                if (role == "Client") role = "User";
                users = users.Where(u => userRoles[u.Id] == role).ToList();
            }

            var sortedUsers = users.OrderBy(u =>
                rolePriority.IndexOf(userRoles[u.Id]) == -1 ? int.MaxValue : rolePriority.IndexOf(userRoles[u.Id])
            ).ToList();

            ViewBag.UserRoles = userRoles;
            return View(sortedUsers);
        }


        //
        [HttpPost]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            var hasActiveOrders = false;

            if (role == "User")
            {
                hasActiveOrders = await _context.Orders
                    .AnyAsync(o => o.UserId == user.Id && o.Status != OrderStatus.Done);
            }
            else if (role == "Driver")
            {
                hasActiveOrders = await _context.Orders
                    .AnyAsync(o => o.DriverId == user.Id && o.Status != OrderStatus.Done);
            }
            else if (role == "Admin")
            {
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.AdminId == user.Id);

                if (restaurant != null)
                {
                    var restaurantProductIds = await _context.RestaurantProducts
                        .Where(rp => rp.RestaurantId == restaurant.Id)
                        .Select(rp => rp.ProductId)
                        .ToListAsync();

                    hasActiveOrders = await _context.OrderProduct
                        .Include(op => op.Order)
                        .AnyAsync(op =>
                            restaurantProductIds.Contains(op.ProductId) &&
                            op.Order.Status != Order.OrderStatus.Done);

                    restaurant.IsDeactivated = true;
                    _context.Restaurants.Update(restaurant);
                    await _context.SaveChangesAsync();
                }
            }


            if (hasActiveOrders)
            {
                TempData["Error"] = $"❌ Cannot deactivate {role?.ToLower()} {user.Email} because there are active orders.";
                return RedirectToAction("ManageUsers");
            }

            user.IsDeactivated = true;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"✅ {role} {user.Email} has been deactivated.";
            return RedirectToAction("ManageUsers");
        }



        //
        [HttpPost]
        public async Task<IActionResult> ActivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsDeactivated = false;
            await _userManager.UpdateAsync(user);

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var restaurant = await _context.Restaurants
                    .FirstOrDefaultAsync(r => r.AdminId == user.Id);

                if (restaurant != null)
                {
                    restaurant.IsDeactivated = false;
                    _context.Restaurants.Update(restaurant);
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = $"✅ User {user.Email} has been activated.";
            return RedirectToAction("ManageUsers");
        }





        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin"))
            {
                TempData["Error"] = "Super Admin cannot be deleted!";
                return RedirectToAction("ManageUsers");
            }

            
            if (roles.Contains("User"))
            {
                bool hasOrders = await _context.Orders.AnyAsync(o => o.UserId == id);
                if (hasOrders)
                {
                    TempData["Error"] = "Cannot delete this client because they have existing orders.";
                    return RedirectToAction("ManageUsers");
                }
            }
            if (roles.Contains("Driver"))
            {
                bool hasOrders = await _context.Orders.AnyAsync(o => o.DriverId == id);
                if (hasOrders)
                {
                    TempData["Error"] = "Cannot delete this driver because they have orders assigned.";
                    return RedirectToAction("ManageUsers");
                }
            }
                var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Error deleting user.";
                return RedirectToAction("ManageUsers");
            }

            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction("ManageUsers");
        }

        public async Task<IActionResult> AssignAdmin(int id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null)
            {
                return NotFound();
            }

            var allUsers = await _userManager.Users.ToListAsync();

            var adminUsers = new List<Users>();
            foreach (var user in allUsers)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    adminUsers.Add(user);
                }
            }

            var model = new AssignAdminViewModel
            {
                RestaurantId = id,
                AvailableAdmins = adminUsers
            };

            return View(model);
        }



        [HttpPost]
        public async Task<IActionResult> AssignAdmin(int restaurantId, string adminId)
        {
            var restaurant = _context.Restaurants.Find(restaurantId);
            if (restaurant == null)
            {
                return NotFound();
            }

            restaurant.AdminId = adminId;
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageRestaurants");
        }


        [HttpGet]
        public async Task<IActionResult> ManageDrivers()
        {
            var allUsers = await _userManager.Users.ToListAsync();

            var drivers = new List<Users>();
            foreach (var user in allUsers)
            {
                if (await _userManager.IsInRoleAsync(user, "Driver"))
                {
                    drivers.Add(user);
                }
            }

            return View(drivers);
        }





        [HttpGet]
        public async Task<IActionResult> ManageAreas()
        {
            var areas = await _context.Areas
                .Where(a => !a.IsDeleted)
                .ToListAsync();
            var setting = await _context.Settings.FirstOrDefaultAsync();

            ViewBag.GlobalDeliveryPrice = setting?.GlobalDeliveryPrice ?? 0;
            return View(areas);
        }

        [HttpPost]
        public async Task<IActionResult> AddArea(string areaName, decimal deliveryPrice)
        {
            if (!string.IsNullOrEmpty(areaName))
            {
                _context.Areas.Add(new Area
                {
                    Name = areaName,
                    DeliveryPrice = deliveryPrice
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageAreas");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteArea(int id)
        {
            var area = await _context.Areas.FindAsync(id);
            if (area != null)
            {
                area.IsDeleted = true;
                _context.Areas.Update(area);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageAreas");
        }
        [HttpPost]
        public async Task<IActionResult> EditDeliveryPrice(int id, decimal newPrice)
        {
            var area = await _context.Areas.FindAsync(id);
            if (area != null && !area.IsDeleted)
            {
                area.DeliveryPrice = newPrice;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageAreas");
        }


        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var restaurantsCount = await _context.Restaurants.CountAsync();
            var adminsCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
            var clientsCount = (await _userManager.GetUsersInRoleAsync("User")).Count;
            var driversCount = (await _userManager.GetUsersInRoleAsync("Driver")).Count;

            var model = new SuperAdminViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                ProfilePictureUrl = user.ProfilePicture ?? "/default-profile.png",
                RestaurantsCount = restaurantsCount,
                AdminsCount = adminsCount,
                ClientsCount = clientsCount,
                DriversCount = driversCount
            };

            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> ManageOrders(string? client, string? from, string? to, string? status)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Driver)
                .AsQueryable();

            if (!string.IsNullOrEmpty(client))
                query = query.Where(o => o.User.FullName.Contains(client));

            if (!string.IsNullOrEmpty(from) && DateTime.TryParse(from, out var fromDate))
                query = query.Where(o => o.Date >= fromDate);

            if (!string.IsNullOrEmpty(to) && DateTime.TryParse(to, out var toDate))
                query = query.Where(o => o.Date <= toDate);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<Order.OrderStatus>(status, out var parsedStatus))
                query = query.Where(o => o.Status == parsedStatus);

            var orders = await query.ToListAsync();

            var rejections = await _context.OrderRejections
                .Include(r => r.Driver)
                .ToListAsync();

            ViewBag.Rejections = rejections;

            return View(orders);
        }




        [HttpPost]
        public async Task<IActionResult> UpdateDeliveryPrice(decimal price)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new Setting { GlobalDeliveryPrice = price };
                _context.Settings.Add(setting);
            }
            else
            {
                setting.GlobalDeliveryPrice = price;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Global delivery price updated.";
            return RedirectToAction("ManageAreas");
        }
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new EditProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FullName = model.FullName;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                Directory.CreateDirectory(uploads);

                var fileName = $"{Guid.NewGuid()}_{model.ImageFile.FileName}";
                var path = Path.Combine(uploads, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await model.ImageFile.CopyToAsync(stream);

                user.ProfilePicture = "/uploads/" + fileName;
            }
            else if (!string.IsNullOrEmpty(model.ProfilePicture))
            {
                user.ProfilePicture = model.ProfilePicture;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
           
            return RedirectToAction("Dashboard", "SuperAdmin");
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Insights(DateTime? fromDate, DateTime? toDate)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.Driver)
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .AsQueryable();

            if (fromDate.HasValue)
                ordersQuery = ordersQuery.Where(o => o.Date >= fromDate);

            if (toDate.HasValue)
                ordersQuery = ordersQuery.Where(o => o.Date <= toDate);

            var ordersPerDay = (await ordersQuery
    .GroupBy(o => o.Date.Date)
    .Select(g => new
    {
        Date = g.Key,
        TotalOrders = g.Count()
    })
    .ToListAsync())
    .Select(o => new DailyOrdersStatViewModel
    {
        Date = o.Date,
        TotalOrders = o.TotalOrders
    }).ToList();

            var topRestaurants = await ordersQuery
                .GroupBy(o => o.OrderProducts
                    .SelectMany(op => op.Product.RestaurantProducts)
                    .FirstOrDefault().Restaurant.Name)
                .Select(g => new TopRestaurantViewModel
                {
                    RestaurantName = g.Key,
                    OrdersCount = g.Count()
                }).OrderByDescending(r => r.OrdersCount)
                .Take(5)
                .ToListAsync();

            
            var topDrivers = await ordersQuery
                .Where(o => o.DriverId != null)
                .GroupBy(o => new { o.Driver.FullName, o.Driver.DeliveryArea, o.Driver.ProfilePictureUrl })
                .Select(g => new TopDriverViewModel
                {
                    DriverName = g.Key.FullName,
                    DeliveredOrders = g.Count(),
                    AreaName = g.Key.DeliveryArea,
                    ProfilePictureUrl = g.Key.ProfilePictureUrl
                })
                .OrderByDescending(d => d.DeliveredOrders)
                .Take(5)
                .ToListAsync();

            var model = new SuperAdminDashboardStatsViewModel
            {
                OrdersPerDay = ordersPerDay,
                TopRestaurants = topRestaurants,
                TopDrivers = topDrivers,
                FromDate = fromDate,
                ToDate = toDate
            };

            return View(model);
        }


    }
}