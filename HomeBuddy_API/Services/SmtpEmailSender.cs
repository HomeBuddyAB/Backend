using HomeBuddy_API.Interfaces.EmailInterfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace HomeBuddy_API.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var host = _config["Email:SmtpHost"];
            var portStr = _config["Email:SmtpPort"];
            var username = _config["Email:SmtpUsername"];
            var password = _config["Email:SmtpPassword"];
            var fromEmail = _config["Email:FromEmail"];
            var fromName = _config["Email:FromName"] ?? "HomeBuddy";
            var useSslStr = _config["Email:UseSsl"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(portStr) ||
                string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException("Email is not configured. Set Email:SmtpHost, Email:SmtpPort, Email:FromEmail (and credentials if needed).");
            }

            if (!int.TryParse(portStr, out var port))
            {
                throw new InvalidOperationException("Email:SmtpPort must be a number.");
            }

            var useSsl = true;
            if (!string.IsNullOrWhiteSpace(useSslStr))
            {
                _ = bool.TryParse(useSslStr, out useSsl);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(toEmail));

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = useSsl,
            };

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            await client.SendMailAsync(message);
        }
    }
}

