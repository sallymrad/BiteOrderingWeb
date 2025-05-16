using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BiteOrderWeb.Models;
using BiteOrderWeb.Data;
using BiteOrderWeb.ViewModels;

[Authorize]
public class ChatController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<Users> _userManager;

    public ChatController(AppDbContext context, UserManager<Users> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("Chat/Order/{orderId}")]
    public async Task<IActionResult> OrderChat(int orderId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Driver)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return NotFound();

        string receiverId = user.Id == order.UserId ? order.DriverId : order.UserId;
        string receiverName = user.Id == order.UserId ? order.Driver?.FullName : order.User?.FullName;

        var messages = await _context.ChatMessages
            .Where(m =>
                ((m.SenderId == user.Id && m.ReceiverId == receiverId) ||
                 (m.SenderId == receiverId && m.ReceiverId == user.Id)) &&
                 m.OrderId == orderId) 
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        ViewBag.OrderId = orderId;
        ViewBag.ReceiverId = receiverId;
        ViewBag.ReceiverName = receiverName;
        return View("Index", messages);
    }

    [HttpGet]
    public async Task<IActionResult> GetLatestMessages(int orderId, string receiverId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var messages = await _context.ChatMessages
            .Where(m =>
                ((m.SenderId == user.Id && m.ReceiverId == receiverId) ||
                 (m.SenderId == receiverId && m.ReceiverId == user.Id)) &&
                 m.OrderId == orderId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return PartialView("_ChatMessagesPartial", messages);
    }



}


