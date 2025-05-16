using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiteOrderWeb.Data;
using BiteOrderWeb.Models;
using System.Linq;
using System.Threading.Tasks;
using BiteOrderWeb.ViewModels;
using Microsoft.AspNetCore.Identity;

public class MenuController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<Users> _userManager;

    public MenuController(AppDbContext context, UserManager<Users> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var admin = await _userManager.GetUserAsync(User);
        var restaurant = await _context.Restaurants
            .Include(r => r.RestaurantProducts)
                .ThenInclude(rp => rp.Product)
                    .ThenInclude(p => p.ProductSizes)
            .FirstOrDefaultAsync(r => r.AdminId == admin.Id);

        if (restaurant == null)
        {
            return NotFound("No restaurant found for this admin.");
        }

        var categoryOrder = new List<string> { "Appetizers", "Main Dishes", "Drinks", "Other" };

        var products = restaurant.RestaurantProducts.Select(rp => rp.Product).Where(p => !p.IsDeactivated || User.IsInRole("Admin")).ToList();

        var groupedProducts = products
            .GroupBy(p => p.Category)
            .OrderBy(g => categoryOrder.IndexOf(g.Key))
            .ThenBy(g => g.Key)
            .ToList();

        return View(groupedProducts);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleProductStatus(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.IsDeactivated = !product.IsDeactivated;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = product.IsDeactivated
            ? "Product has been deactivated successfully."
            : "Product has been activated successfully.";

        return RedirectToAction("Index");
    }


    [HttpGet]
    public IActionResult AddProduct()
    {
        return View(new ProductViewModel
        {
            Sizes = new List<ProductSizeViewModel>
        {
            new ProductSizeViewModel { Size = "Small", Quantity = 0 },
            new ProductSizeViewModel { Size = "Medium", Quantity = 0 },
            new ProductSizeViewModel { Size = "Large", Quantity = 0 }
        }
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct(ProductViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);


        var admin = await _userManager.GetUserAsync(User);


        //
        var restaurant = await _context.Restaurants
        .FirstOrDefaultAsync(r => r.AdminId == admin.Id);

        var category = model.Category;
        if (category == "Other")
        {
            category = Request.Form["NewCategory"];
        }

        string uniqueFileName = null;

        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            uniqueFileName = $"{Guid.NewGuid()}_{model.ImageFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.ImageFile.CopyToAsync(fileStream);
            }
        }

        var product = new Product
        {
            Name = model.Name,
            Category = category,
            Quantity = model.Quantity,
            Description = model.Description,
            ImageUrl = uniqueFileName != null ? "/uploads/" + uniqueFileName : null
        };



        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        //
        var restaurantProduct = new RestaurantProduct
        {
            RestaurantId = restaurant.Id,
            ProductId = product.Id
        };
        _context.RestaurantProducts.Add(restaurantProduct);

        foreach (var size in model.Sizes)
        {
            var productSize = new ProductSize
            {
                ProductId = product.Id,
                Size = size.Size,
                Quantity = size.Quantity,
                Price = size.Price
            };
            _context.ProductSizes.Add(productSize);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.ProductSizes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

      
        if (product.ProductSizes == null)
        {
            product.ProductSizes = new List<ProductSize>();
        }

        var viewModel = new ProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Category = product.Category,
            ImageUrl = product.ImageUrl,
            Sizes = product.ProductSizes.Select(s => new ProductSizeViewModel
            {
                Id = s.Id,
                Size = s.Size,
                Quantity = s.Quantity,
                Price = s.Price
            }).ToList()
        };

        return View(viewModel);
    }


    [HttpPost]
    public async Task<IActionResult> EditProduct(ProductViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        /////
        if (model.Sizes.Count == 0)
        {
            ModelState.AddModelError("", "Please add at least one size before updating the product.");
            return View(model);
        }



        var product = await _context.Products
            .Include(p => p.ProductSizes)
            .FirstOrDefaultAsync(p => p.Id == model.Id);

        if (product == null) return NotFound();

        product.Name = model.Name;
        product.Description = model.Description;
        product.Category = model.Category;


        var category = model.Category;
        if (category == "Other")
        {
            category = Request.Form["NewCategory"];
        }
        product.Category = category;



        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{model.ImageFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.ImageFile.CopyToAsync(fileStream);
            }

            product.ImageUrl = "/uploads/" + uniqueFileName;
        }

       
        var existingSizeIds = model.Sizes.Select(s => s.Id).ToList();
        var sizesToRemove = product.ProductSizes.Where(s => !existingSizeIds.Contains(s.Id)).ToList();
        _context.ProductSizes.RemoveRange(sizesToRemove);

        
        foreach (var sizeVM in model.Sizes)
        {
            var existingSize = product.ProductSizes.FirstOrDefault(s => s.Id == sizeVM.Id);
            if (existingSize != null)
            {
                existingSize.Size = sizeVM.Size;
                existingSize.Quantity = sizeVM.Quantity;
                existingSize.Price = sizeVM.Price;
            }
            else if (sizeVM.Id == 0) 
            {
                product.ProductSizes.Add(new ProductSize
                {
                    Size = sizeVM.Size,
                    Quantity = sizeVM.Quantity,
                    Price = sizeVM.Price,
                    ProductId = product.Id
                });
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }





}