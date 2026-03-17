namespace HomeBuddy_API.Interfaces.EmailInterfaces
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}

