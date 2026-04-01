using HomeBuddy_API.Interfaces.EmailInterfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace HomeBuddy_API.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
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
                _logger.LogWarning(
                    "Email not sent (SMTP not configured). Set Email:SmtpHost, Email:SmtpPort, Email:FromEmail. To={To}, Subject={Subject}",
                    toEmail,
                    subject);
                return;
            }

            if (!int.TryParse(portStr, out var port))
            {
                _logger.LogError("Email:SmtpPort must be a number.");
                return;
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

            _ = ct;
            await client.SendMailAsync(message);
        }
    }
}
