using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiteOrderWeb.Data;
using BiteOrderWeb.Models;
using BiteOrderWeb.ViewModels;

namespace BiteOrderWeb.Controllers
{
    [Authorize(Roles = "User")]
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public PaymentController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public async Task<IActionResult> Index(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);
            if (order == null) return NotFound();

            var paymentViewModel = new PaymentViewModel
            {
                OrderId = order.Id,
                Amount = order.TotalPrice,
                Date = DateTime.UtcNow
            };

            return View(paymentViewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Confirm(PaymentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == model.OrderId && o.UserId == user.Id);
            if (order == null) return NotFound();

            var payment = new Payment
            {
                UserId = user.Id,
                OrderId = model.OrderId,
                Amount = model.Amount,
                PaymentIntentId = model.PaymentIntentId,
                Status = model.Status ?? "Pending",
                Date = model.Date
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();


            return RedirectToAction("Success");
        }


        public IActionResult Success()
        {
            return View();
        }

      
        }

    }

