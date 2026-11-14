using System.Net;
using System.Net.Mail;

namespace PawCare.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

public class SmtpEmailService(IConfiguration config, IHostEnvironment env, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string body)
    {
        var host = config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            if (!env.IsDevelopment())
                throw new InvalidOperationException("SMTP is not configured.");

            // Development: log the email instead of sending it.
            logger.LogWarning("SMTP is not configured. Email to {To}\nSubject: {Subject}\n{Body}", to, subject, body);
            return;
        }

        using var client = new SmtpClient(host, config.GetValue("Smtp:Port", 587))
        {
            EnableSsl = config.GetValue("Smtp:EnableSsl", true)
        };

        var user = config["Smtp:User"];
        if (!string.IsNullOrWhiteSpace(user))
            client.Credentials = new NetworkCredential(user, config["Smtp:Password"]);

        var from = config["Smtp:From"] ?? "no-reply@pawcare.example";
        using var message = new MailMessage(from, to, subject, body);
        await client.SendMailAsync(message);
    }
}
