using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiteOrderWeb.Data;
using BiteOrderWeb.Models;
using System.Linq;
using System.Threading.Tasks;
using BiteOrderWeb.ViewModels;
using Stripe.Checkout;
using Stripe;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity.UI.Services;
using BiteOrderWeb.Services;

[Authorize]
public class ShoppingCartController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<Users> _userManager;
    private readonly EmailSenderService _emailSender;
    private readonly IServiceScopeFactory _scopeFactory;

    public ShoppingCartController(AppDbContext context, UserManager<Users> userManager, EmailSenderService emailSender, IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _userManager = userManager;
        _emailSender = emailSender;
        _scopeFactory = scopeFactory;
    }
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var cart = await _context.ShoppingCarts
            .Include(c => c.ShoppingCartProducts)
                .ThenInclude(cp => cp.Product)
                    .ThenInclude(p => p.ProductSizes)
            .Include(c => c.ShoppingCartProducts)
                .ThenInclude(cp => cp.Product)
                    .ThenInclude(p => p.RestaurantProducts)
                        .ThenInclude(rp => rp.Restaurant)
            .FirstOrDefaultAsync(c => c.UserId == user.Id);

        return View(cart);
    }


    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, string size, int quantity, string notes)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var product = await _context.Products.FindAsync(productId);
        var productSize = await _context.ProductSizes
    .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.Size == size);

        if (productSize == null)
        {
            return NotFound("Product size not found");
        }

        if (quantity > productSize.Quantity)
        {
            TempData["Error"] = "Not enough quantity available.";
            return RedirectToAction("Index");
        }

        if (product == null) return NotFound("Product not found");

        var cart = await _context.ShoppingCarts
            .Include(c => c.ShoppingCartProducts)
            .FirstOrDefaultAsync(c => c.UserId == user.Id);

        if (cart == null)
        {
            cart = new ShoppingCart { UserId = user.Id };
            _context.ShoppingCarts.Add(cart);
            await _context.SaveChangesAsync();
        }

        var cartProduct = cart.ShoppingCartProducts
            .FirstOrDefault(cp => cp.ProductId == productId && cp.Size == size);

        if (cartProduct == null)
        {
            cart.ShoppingCartProducts.Add(new ShoppingCartProduct
            {
                ProductId = productId,
                Quantity = quantity,
                Size = size,
                Notes = notes

            });
        }
        else
        {
            cartProduct.Quantity += quantity;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }



    [HttpPost]
    public async Task<IActionResult> RemoveFromCart(int productId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var cart = await _context.ShoppingCarts
            .Include(c => c.ShoppingCartProducts)
            .FirstOrDefaultAsync(c => c.UserId == user.Id);

        if (cart == null) return NotFound();

        var cartProduct = cart.ShoppingCartProducts
            .FirstOrDefault(cp => cp.ProductId == productId);

        if (cartProduct != null)
        {
            cart.ShoppingCartProducts.Remove(cartProduct);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> GetCartCount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Json(0);

        var cart = await _context.ShoppingCarts
            .Include(c => c.ShoppingCartProducts)
            .FirstOrDefaultAsync(c => c.UserId == user.Id);

        int itemCount = cart?.ShoppingCartProducts.Sum(cp => cp.Quantity) ?? 0;
        return Json(itemCount);
    }



    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int ProductId, string Size, int Quantity)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var cart = await _context.ShoppingCarts
            .Include(c => c.ShoppingCartProducts)
                .ThenInclude(p => p.Product)
                .ThenInclude(p => p.ProductSizes)
            .FirstOrDefaultAsync(c => c.UserId == user.Id);

        if (cart == null) return NotFound();

        var item = cart.ShoppingCartProducts
            .FirstOrDefault(p => p.ProductId == ProductId && p.Size == Size);

        if (item != null && Quantity > 0)
        {
            var availableQuantity = item.Product.ProductSizes
                .FirstOrDefault(ps => ps.Size == Size)?.Quantity ?? 0;

            if (Quantity > availableQuantity)
            {
                if (availableQuantity == 0)
                    TempData["Error"] = $"❌ Out of stock for {item.Product.Name} ({Size})";
                else
                    TempData["Error"] = $"❌ Only {availableQuantity} left for {item.Product.Name} ({Size})";

                return RedirectToAction("Index");
            }

            item.Quantity = Quantity;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Quantity updated successfully!";
        }

        return RedirectToAction("Index");
    }


    [HttpPost]
    public async Task<IActionResult> ClearCart()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var cart = await _context.ShoppingCarts
            .Include(c => c.ShoppingCartProducts)
            .FirstOrDefaultAsync(c => c.UserId == user.Id);

        if (cart == null) return NotFound();

        _context.ShoppingCartProducts.RemoveRange(cart.ShoppingCartProducts);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var areas = await _context.Areas
            .Where(a => !a.IsDeleted)
            .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name })
            .ToListAsync();

        return View(new CheckoutViewModel { Areas = areas });
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (!ModelState.IsValid)
        {
            model.Areas = await _context.Areas
                 .Where(a => !a.IsDeleted)
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name })
                .ToListAsync();

            return View(model);
        }

        var cart = await _context.ShoppingCarts
            .Include(c => c.ShoppingCartProducts)
            .ThenInclude(p => p.Product)
            .ThenInclude(p => p.ProductSizes)
            .FirstOrDefaultAsync(c => c.UserId == user.Id);

        if (cart == null || !cart.ShoppingCartProducts.Any())
            return RedirectToAction("Index");


        //
        var firstProductId = cart.ShoppingCartProducts.First().ProductId;

        var restaurantProduct = await _context.RestaurantProducts
            .Include(rp => rp.Restaurant)
            .FirstOrDefaultAsync(rp => rp.ProductId == firstProductId);

        var restaurant = restaurantProduct?.Restaurant;

        if (restaurant != null)
        {
            var now = DateTime.Now.TimeOfDay;
            var isOpen = restaurant.OpeningTime < restaurant.ClosingTime
                ? now >= restaurant.OpeningTime && now <= restaurant.ClosingTime
                : now >= restaurant.OpeningTime || now <= restaurant.ClosingTime;

            if (!isOpen)
            {
                TempData["RestaurantClosed"] = "Sorry, the restaurant is currently closed. Please try again during its working hours.";
                return RedirectToAction("Index");
            }

        }
        foreach (var item in cart.ShoppingCartProducts)
        {
            var productSize = await _context.ProductSizes
                .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId && ps.Size == item.Size);

            if (productSize == null || item.Quantity > productSize.Quantity)
            {
                TempData["Error"] = $"Not enough quantity for {item.Product?.Name}.";
                return RedirectToAction("Index");
            }
        }

        var totalPrice = cart.ShoppingCartProducts.Sum(p => (p.SizePrice ?? 0) * p.Quantity);

        var address = new BiteOrderWeb.Models.Address
        {
            Street = model.Street,
            Building = model.Building,
            Floor = model.Floor,
            AreaId = model.AreaId
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();
        var setting = await _context.Settings.FirstOrDefaultAsync();
        var deliveryPrice = setting?.GlobalDeliveryPrice ?? 0;
        var area = await _context.Areas.FindAsync(model.AreaId);

        var areaName = area?.Name;

        var order = new Order
        {
            UserId = user.Id,
            Date = DateTime.UtcNow,
            Status = Order.OrderStatus.Pending,
            TotalPrice = totalPrice + deliveryPrice,
            DeliveryPrice = deliveryPrice,
            AddressId = address.Id,
            OrderProducts = cart.ShoppingCartProducts.Select(cp => new OrderProduct
            {
                ProductId = cp.ProductId,
                Quantity = cp.Quantity,
                Size = cp.Size,
                Notes = cp.Notes
            }).ToList()
        };

        foreach (var item in order.OrderProducts)
        {
            var productSize = await _context.ProductSizes
                .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId && ps.Size == item.Size);

            if (productSize != null)
            {
                productSize.Quantity -= item.Quantity;
            }
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return RedirectToAction("Payment", new { orderId = order.Id });

    }


    [HttpGet]
    public async Task<IActionResult> Payment(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return NotFound();

        var vm = new PaymentViewModel
        {
            OrderId = orderId,
            Amount = order.TotalPrice
        };

        return View(vm);
    }
    [HttpPost]
    public async Task<IActionResult> ProcessPayment(int orderId, decimal amount)
    {

        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound();


        var user = await _userManager.GetUserAsync(User);
        var cart = await _context.ShoppingCarts
            .Include(c => c.ShoppingCartProducts)
            .FirstOrDefaultAsync(c => c.UserId == user.Id);

        if (cart != null)
        {
            _context.ShoppingCartProducts.RemoveRange(cart.ShoppingCartProducts);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("PaymentSuccess");
    }
    [HttpPost]
    public async Task<IActionResult> CreateStripeSession(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderProducts)
            .ThenInclude(op => op.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.TotalPrice <= 0)
            return BadRequest("Invalid order");

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(order.TotalPrice * 100),
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "BiteOrder Purchase"
                    }
                },
                Quantity = 1
            }
        },
            Mode = "payment",

           
            SuccessUrl = $"{Request.Scheme}://{Request.Host}/ShoppingCart/PaymentSuccess?sessionId={{CHECKOUT_SESSION_ID}}&orderId={order.Id}",

            CancelUrl = Url.Action("Index", "ShoppingCart", null, Request.Scheme)
        };

        var service = new SessionService();
        var session = service.Create(options);

        return Redirect(session.Url);
    }


    [Authorize(Roles = "User")]
    [HttpGet]
    public async Task<IActionResult> PaymentSuccess(string sessionId, int orderId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var service = new SessionService();
        Session session = await service.GetAsync(sessionId);

        if (session.PaymentStatus == "paid")
        {
            var order = await _context.Orders
    .Include(o => o.Address) 
    .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            var payment = new Payment
            {
                UserId = user.Id,
                OrderId = order.Id,
                Amount = order.TotalPrice,
                Status = "Paid",
                PaymentIntentId = session.PaymentIntentId,
                Date = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

          
            var cart = await _context.ShoppingCarts
                .Include(c => c.ShoppingCartProducts)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (cart != null)
            {
                cart.ShoppingCartProducts.Clear();
            }

            await _context.SaveChangesAsync();

            var areaName = _context.Areas
             .Where(a => a.Id == order.Address.AreaId)
             .Select(a => a.Name)
             .FirstOrDefault();

            var drivers = await _context.Users
                .Where(u => u.DeliveryArea.ToLower() == areaName.ToLower())
                .ToListAsync();

            foreach (var driver in drivers)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(driver.Email))
                    {
                        string subject = "📦 New Order in Your Area";
                        string body = $@"Hi {driver.FullName},<br/>" +
                                      $"A new order just came from area <b>{areaName}</b>.<br/>" +
                                      $"Total: ${order.TotalPrice}<br/>" +
                                      $"Customer: {user.FullName} - {user.Email}<br/>" +
                                      $"Click <a href='https://yourdomain.com/Driver/Orders'>here</a> to view it.";

                        await _emailSender.SendEmailAsync(driver.Email, subject, body);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to send to {driver.Email}: {ex.Message}");
                }
            }

            _ = Task.Run(async () =>
            {
                const int maxReminders = 3;
                int remindersSent = 0;

                while (remindersSent < maxReminders)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    remindersSent++;

                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var emailService = scope.ServiceProvider.GetRequiredService<EmailSenderService>();

                    var updatedOrder = await context.Orders
                        .Include(o => o.Address)
                        .FirstOrDefaultAsync(o => o.Id == order.Id);

                    if (updatedOrder != null && updatedOrder.DriverId == null && updatedOrder.Status == Order.OrderStatus.Pending)
                    {
                        var rejectedDrivers = await context.OrderRejections
                            .Where(r => r.OrderId == order.Id)
                            .Select(r => r.DriverId)
                            .ToListAsync();

                        var driversInArea = await context.Users
                            .Where(u => u.DeliveryArea.ToLower() == areaName.ToLower() && !rejectedDrivers.Contains(u.Id))
                            .ToListAsync();

                        foreach (var driver in driversInArea)
                        {
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(driver.Email))
                                {
                                    string subject = "🔁 Reminder: Order still pending in your area";
                                    string body = $@"Hi {driver.FullName},<br/>
                            Order #{order.Id} is still pending in <b>{areaName}</b>.<br/>
                            Click <a href='https://yourdomain.com/Driver/Orders'>here</a> to accept.";

                                    await emailService.SendEmailAsync(driver.Email, subject, body);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Reminder failed: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        break; 
                    }
                }
            });




            return View("PaymentSuccess");
        }

        return View("PaymentFailed");
    }


}