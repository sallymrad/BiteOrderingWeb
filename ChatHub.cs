using BiteOrderWeb.Data;
using BiteOrderWeb.Models;
using BiteOrderWeb.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BiteOrderWeb.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly EmailSenderService _emailSender;

        public ChatHub(AppDbContext context, EmailSenderService emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var orderId = httpContext?.Request.Query["orderId"];
            var userId = httpContext?.Request.Query["userId"];

            if (!string.IsNullOrEmpty(orderId) && !string.IsNullOrEmpty(userId))
            {
                string groupName = $"order-{orderId}-{userId}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            }

            await base.OnConnectedAsync();
        }



        public async Task SendMessage(string senderId, string receiverId, string message, int orderId)
        {
            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
                SentAt = DateTime.UtcNow,
                OrderId = orderId
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

           
            string groupName = $"order-{orderId}-{receiverId}";
            await Clients.Group(groupName).SendAsync("ReceiveMessage", senderId, message, orderId);

           
            string emailRecipientId = receiverId;  

           
            var receiverEmail = await GetReceiverEmail(emailRecipientId);

            if (string.IsNullOrEmpty(receiverEmail))
            {
               
                return;
            }

            var subject = "New Message in Your Order";
            var body = $"You have a new message from the order #{orderId}. Message: {message}";

           
            await _emailSender.SendEmailAsync(receiverEmail, subject, body);
        }

       
        private async Task<string> GetReceiverEmail(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return user?.Email ?? string.Empty;
        }

    }
}