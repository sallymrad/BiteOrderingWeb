using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiteOrderWeb.Data;
using BiteOrderWeb.Models;
using BiteOrderWeb.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using static BiteOrderWeb.Models.Order;

public class ClientController : Controller
{
    private readonly UserManager<Users> _userManager;
    private readonly AppDbContext _context;
    private readonly SignInManager<Users> _signInManager;
    private readonly IWebHostEnvironment _environment;
    public ClientController(SignInManager<Users> signInManager, UserManager<Users> userManager, AppDbContext context, IWebHostEnvironment environment)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        _environment = environment;
    }
    public async Task<IActionResult> Index()
    {
        var topProductIds = await _context.OrderProduct
            .GroupBy(op => op.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(20)
            .ToListAsync();

        var topRestaurants = await _context.RestaurantProducts
            .Include(rp => rp.Restaurant)
            .Where(rp =>
        !rp.Restaurant.IsDeactivated &&  
        topProductIds.Select(x => x.ProductId).Contains(rp.ProductId))
            .GroupBy(rp => rp.Restaurant)
            .Select(g => new
            {
                Restaurant = g.Key,
                ProductCount = g.Count()
            })
            .OrderByDescending(x => x.ProductCount)
            .Take(3)
            .ToListAsync();

        var model = new HomePageViewModel
        {
            TopRestaurants = topRestaurants.Select(x => x.Restaurant).ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> Category(string name)
    {
        var products = await _context.Products
            .Include(p => p.RestaurantProducts)
                .ThenInclude(rp => rp.Restaurant)
            .Where(p =>
                p.Category.Replace(" ", "").ToLower() == name.Replace(" ", "").ToLower()
                && !p.IsDeactivated
                && p.RestaurantProducts.Any(rp => rp.Restaurant != null && !EF.Property<bool>(rp.Restaurant, "IsDeactivated")))
            .Select(p => new
            {
                Product = p,
                OrdersCount = _context.OrderProduct.Count(op => op.ProductId == p.Id)
            })
            .OrderByDescending(p => p.OrdersCount)
            .Select(p => p.Product)
            .ToListAsync();

        return View("Category", products);
    }


    [HttpGet]
    public async Task<IActionResult> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Json(new List<RestaurantSearchViewModel>());

        var restaurants = await _context.Restaurants
            .Where(r => !r.IsDeactivated && r.Name.ToLower().Contains(query.ToLower())) 
            .Select(r => new RestaurantSearchViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                ImageUrl = r.ImageUrl
            })
            .ToListAsync();

        return Json(restaurants);
    }

    public async Task<IActionResult> Restaurant(int id)
    {
        var restaurant = await _context.Restaurants
            .Include(r => r.Address)
            .Include(r => r.RestaurantProducts)
                .ThenInclude(rp => rp.Product)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeactivated);

        if (restaurant == null)
            return NotFound();

      
        restaurant.RestaurantProducts = restaurant.RestaurantProducts
            .Where(rp => rp.Product != null && !rp.Product.IsDeactivated)
            .ToList();

        return View(restaurant);
    }


    public async Task<IActionResult> ProductDetails(int id)
    {
        var product = await _context.Products
        .Include(p => p.ProductSizes)
        .Include(p => p.RestaurantProducts)
            .ThenInclude(rp => rp.Restaurant)
        .FirstOrDefaultAsync(p =>
            p.Id == id && !p.IsDeactivated &&
            p.RestaurantProducts.Any(rp => rp.Restaurant != null && !rp.Restaurant.IsDeactivated)
        );

        if (product == null)
            return NotFound();

        var viewModel = new ProductDetailsViewModel
        {
            ProductId = product.Id,
            Name = product.Name,
            ImageUrl = product.ImageUrl,
            Description = product.Description,
            Sizes = product.ProductSizes.ToList(),
            RestaurantName = product.RestaurantProducts
        .FirstOrDefault(rp => rp.Restaurant != null)?.Restaurant?.Name
        };


        return View(viewModel);
    }


    [HttpPost]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> AddToCart(int ProductId, string SelectedSize, int Quantity, string? Notes)
    {
        var userId = _userManager.GetUserId(User);

        var restaurantProduct = await _context.RestaurantProducts
            .Include(rp => rp.Restaurant)
            .Include(rp => rp.Product)
                .ThenInclude(p => p.ProductSizes)
            .FirstOrDefaultAsync(rp => rp.ProductId == ProductId);

        if (restaurantProduct == null || restaurantProduct.Product.IsDeactivated)
            return NotFound();

        var product = restaurantProduct.Product;
        var productRestaurantId = restaurantProduct.RestaurantId;

        var availableQuantity = product.ProductSizes
            .FirstOrDefault(ps => ps.Size == SelectedSize)?.Quantity ?? 0;

        var cart = await _context.ShoppingCarts
            .Include(c => c.ShoppingCartProducts)
            .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new ShoppingCart
            {
                UserId = userId,
                ShoppingCartProducts = new List<ShoppingCartProduct>()
            };
            _context.ShoppingCarts.Add(cart);
        }
        else
        {
            var restaurantIdsInCart = cart.ShoppingCartProducts
                .Select(p => _context.RestaurantProducts
                    .Where(rp => rp.ProductId == p.ProductId)
                    .Select(rp => rp.RestaurantId)
                    .FirstOrDefault())
                .Distinct()
                .ToList();

            if (restaurantIdsInCart.Any() && !restaurantIdsInCart.Contains(productRestaurantId))
            {
                TempData["Error"] = "You cannot order from more than one restaurant in the same cart. Please clear your cart first.";
                return RedirectToAction("Index", "ShoppingCart");
            }
        }

        var existingItem = cart.ShoppingCartProducts
            .FirstOrDefault(p => p.ProductId == ProductId && p.Size == SelectedSize);

        int currentInCart = existingItem?.Quantity ?? 0;
        int totalRequested = currentInCart + Quantity;

        if (totalRequested > availableQuantity)
        {
            int left = availableQuantity - currentInCart;

            if (left <= 0)
                TempData["Error"] = $"❌ Out of stock for {product.Name} ({SelectedSize})";
            else
                TempData["Error"] = $"❌ Only {left} left for {product.Name} ({SelectedSize})";

            return RedirectToAction("Index", "ShoppingCart");
        }

      
        if (existingItem != null)
        {
            existingItem.Quantity += Quantity;
        }
        else
        {
            var newItem = new ShoppingCartProduct
            {
                ShoppingCartId = cart.Id,
                ProductId = ProductId,
                Size = SelectedSize,
                Quantity = Quantity,
                Notes = Notes
            };

            cart.ShoppingCartProducts.Add(newItem);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "The product has been added to your cart!";
        return RedirectToAction("Index", "ShoppingCart");
    }

    [Authorize(Roles = "User")]
    public async Task<IActionResult> MyOrders()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var orders = await _context.Orders
            .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
            .Include(o => o.Address)
                .ThenInclude(a => a.Area)
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.Date)
            .ToListAsync();

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var model = new ClientSettingsViewModel
        {
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
        TempData.Remove("AccountDeactivated");

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSettings(ClientSettingsViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (!ModelState.IsValid) return View("Settings", model);

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;

      
        if (!string.IsNullOrEmpty(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View("Settings", model);
            }
            TempData["PasswordUpdated"] = "Password reset successfully!";

        }


        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View("Settings", model);
        }

        TempData["Success"] = "Settings updated successfully.";
        return RedirectToAction("Settings");
    }


    [HttpPost]
    public async Task<IActionResult> UploadProfilePicture(IFormFile profileImage)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || profileImage == null) return RedirectToAction("Settings");

        var fileName = Guid.NewGuid() + Path.GetExtension(profileImage.FileName);
        var path = Path.Combine(_environment.WebRootPath, "uploads", fileName);

        using (var stream = new FileStream(path, FileMode.Create))
        {
            await profileImage.CopyToAsync(stream);
        }

        user.ProfilePictureUrl = "/uploads/" + fileName;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Profile picture updated.";
        return RedirectToAction("Settings");
    }
    [HttpGet]
    public async Task<IActionResult> CheckCanDeactivate()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { canDeactivate = false });

        var hasOrders = await _context.Orders
            .AnyAsync(o => o.UserId == user.Id && o.Status != OrderStatus.Done);

        return Json(new { canDeactivate = !hasOrders });
    }
 
    [HttpPost]
    public async Task<IActionResult> DeactivateAccount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var orders = await _context.Orders
            .Where(o => o.UserId == user.Id && o.Status != OrderStatus.Done)
            .ToListAsync();

        if (orders.Any())
        {
            TempData["HasOrders"] = true;
            return RedirectToAction("Settings");
        }

        var cart = await _context.ShoppingCarts
            .Include(c => c.ShoppingCartProducts)
            .FirstOrDefaultAsync(c => c.UserId == user.Id);

        if (cart != null)
            _context.ShoppingCartProducts.RemoveRange(cart.ShoppingCartProducts);

        user.IsDeactivated = true;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        await _signInManager.SignOutAsync();
        TempData["AccountDeactivated"] = "Your account has been deactivated. You cannot log in again.";

        return RedirectToAction("Index", "Home");
    }



}








