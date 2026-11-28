using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using MimeKit;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PawCare;

public class Helper(IHttpContextAccessor ct, IConfiguration cfg)
{
    private readonly PasswordHasher<object> ph = new();

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);
    }

    public bool VerifyPassword(string hash, string password)
    {
        return ph.VerifyHashedPassword(0, hash, password)
                == PasswordVerificationResult.Success;
    }

    public void SignIn(string id, string name, string email, string role, bool rememberMe)
    {
        List<Claim> claims = [
            new(ClaimTypes.NameIdentifier, id),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
        ];
        ClaimsIdentity identity = new(claims, "Cookies");
        ClaimsPrincipal principal = new(identity);

        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,
        };

        ct.HttpContext!.SignInAsync(principal, properties);
    }

    public void SignOut()
    {
        ct.HttpContext!.SignOutAsync();
    }

    public string RandomToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    public string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public void SendEmail(string to, string subject, string body)
    {
        var host = cfg["Smtp:Host"];
        if (string.IsNullOrEmpty(host))
        {
            Console.WriteLine($"[EMAIL] To: {to}\nSubject: {subject}\n{body}");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(cfg["Smtp:From"] ?? "no-reply@pawcare.example"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        var port = cfg.GetValue("Smtp:Port", 587);
        var ssl = cfg.GetValue("Smtp:EnableSsl", true);

        var security = !ssl ? SecureSocketOptions.None
                     : port == 465 ? SecureSocketOptions.SslOnConnect
                     : SecureSocketOptions.StartTls;

        using var client = new SmtpClient();
        client.Connect(host, port, security);

        var user = cfg["Smtp:User"];
        if (!string.IsNullOrEmpty(user))
        {
            client.Authenticate(user, cfg["Smtp:Password"]);
        }

        client.Send(message);
        client.Disconnect(true);
    }

    public string RecaptchaSiteKey => cfg["Recaptcha:SiteKey"] ?? "";

    public bool RecaptchaEnabled =>
        RecaptchaSiteKey != "" && (cfg["Recaptcha:SecretKey"] ?? "") != "";

    public bool VerifyRecaptcha(string? response)
    {
        if (!RecaptchaEnabled) return true;
        if (string.IsNullOrEmpty(response)) return false;

        try
        {
            using var http = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://www.google.com/recaptcha/api/siteverify")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = cfg["Recaptcha:SecretKey"]!,
                    ["response"] = response
                })
            };
            using var result = http.Send(request);
            using var json = JsonDocument.Parse(result.Content.ReadAsStream());
            return json.RootElement.GetProperty("success").GetBoolean();
        }
        catch (Exception)
        {
            return false;
        }
    }
}