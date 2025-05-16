using BiteOrderWeb.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace BiteOrderWeb.Services
{
    public class EmailSenderService
    {
        private readonly EmailSettings _emailSettings;

        public EmailSenderService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var mail = new MailMessage
            {
                From = new MailAddress(_emailSettings.From),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            using var smtp = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port)
            {
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                EnableSsl = true
            };

            await smtp.SendMailAsync(mail);
        }
    }
}
