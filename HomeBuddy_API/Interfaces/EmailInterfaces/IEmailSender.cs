namespace HomeBuddy_API.Interfaces.EmailInterfaces
{
    public interface IEmailSender
    {
        /// <summary>Sends an HTML email. Implementations may no-op when SMTP is not configured (e.g. local dev).</summary>
        Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
    }
}
