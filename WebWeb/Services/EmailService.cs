using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace WebWeb.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrEmpty(toEmail)) return;

            var smtpServer = _config["EmailSettings:SmtpServer"];
            var port = int.Parse(_config["EmailSettings:Port"] ?? "587");
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var senderName = _config["EmailSettings:SenderName"];
            var username = _config["EmailSettings:Username"];
            var password = _config["EmailSettings:Password"];

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(senderEmail, senderName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using (var client = new SmtpClient(smtpServer, port))
                {
                    client.Credentials = new NetworkCredential(username, password);
                    client.EnableSsl = true;
                    await client.SendMailAsync(message);
                }
            }
        }
    }
}