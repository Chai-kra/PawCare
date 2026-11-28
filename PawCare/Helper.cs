using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace PawCare;

public class Helper(IHttpContextAccessor ct, IConfiguration cfg, IWebHostEnvironment en)
{
    // ------------------------------------------------------------------------
    // Security Helper Functions
    // ------------------------------------------------------------------------

    private readonly PasswordHasher<object> ph = new();

    // Verified when no real hash exists, so "unknown account" costs the same time as "wrong password".
    private static readonly string DummyHash =
        new PasswordHasher<object>().HashPassword(0, "timing-equaliser-not-a-real-password");

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);
    }

    public bool VerifyPassword(string? hash, string password)
    {
        if (string.IsNullOrEmpty(hash))
        {
            ph.VerifyHashedPassword(0, DummyHash, password);
            return false;
        }

        return ph.VerifyHashedPassword(0, hash, password)
                == PasswordVerificationResult.Success;
    }

    // NameIdentifier = account id (customer "C…", staff "S…")
    // Name           = display name (shown in the header)
    // Role           = Customer, Staff or Admin
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

    // Re-issues the cookie with fresh claims (e.g. after the display name changes),
    // keeping the user's existing "remember me" choice.
    public void RefreshSignIn(string id, string name, string email, string role)
    {
        var current = ct.HttpContext!.AuthenticateAsync().GetAwaiter().GetResult();
        SignIn(id, name, email, role, current.Properties?.IsPersistent ?? false);
    }

    public void SignOut()
    {
        ct.HttpContext!.SignOutAsync();
    }

    // Random token for email links. Only its hash is stored in the database.
    public string RandomToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    public string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    // True if a stored token hash matches the token from the link and has not expired.
    // Used for both email verification and password reset.
    public bool IsTokenValid(string? storedHash, DateTime? expires, string? token)
    {
        if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(token)) return false;
        if (expires == null || expires < DateTime.UtcNow) return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(storedHash),
            Encoding.UTF8.GetBytes(HashToken(token)));
    }

    // Absolute URL for emailed links. If App:BaseUrl is set (production) the host is fixed,
    // so a forged Host header cannot point a reset link at another site.
    public string PublicUrl(string path)
    {
        var baseUrl = cfg["App:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
            return baseUrl.TrimEnd('/') + path;

        var req = ct.HttpContext!.Request;
        return $"{req.Scheme}://{req.Host}{path}";
    }

    // ------------------------------------------------------------------------
    // Phone
    // ------------------------------------------------------------------------

    // Empty input is valid and gives null. Otherwise: optional leading +, 8-14 digits
    // (spaces and hyphens are removed). At most 15 chars, which fits Customer.Phone.
    public bool TryNormalizePhone(string? input, out string? phone)
    {
        phone = null;
        if (string.IsNullOrWhiteSpace(input)) return true;

        var s = Regex.Replace(input, @"[ \-]", "");
        if (!Regex.IsMatch(s, @"^\+?[0-9]{8,14}$")) return false;

        phone = s;
        return true;
    }

    // ------------------------------------------------------------------------
    // Photo Upload (ImageSharp)
    // ------------------------------------------------------------------------

    private const long MaxPhotoBytes = 1 * 1024 * 1024;
    private const long MaxPhotoPixels = 25_000_000;   // guards against huge-dimension images that are small on disk

    private static readonly Regex PhotoType = new(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);
    private static readonly Regex PhotoName = new(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

    // Returns "" if the upload is acceptable, otherwise an error message.
    // Cheap checks first (type, extension, size), then the file header is actually parsed,
    // because content type and extension are supplied by the client and can lie.
    public string ValidatePhoto(IFormFile f)
    {
        if (!PhotoType.IsMatch(f.ContentType) || !PhotoName.IsMatch(f.FileName))
            return "Only JPG and PNG photo is allowed.";

        if (f.Length > MaxPhotoBytes)
            return "Photo size cannot be more than 1MB.";

        try
        {
            using var stream = f.OpenReadStream();
            var info = SixLabors.ImageSharp.Image.Identify(stream);   // reads the header only; throws if unreadable

            var format = info.Metadata.DecodedImageFormat;
            if (format != JpegFormat.Instance && format != PngFormat.Instance)
                return "Only JPG and PNG photo is allowed.";

            if ((long)info.Width * info.Height > MaxPhotoPixels)
                return "Photo dimensions are too large.";
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Untrusted input: any decoder failure means "reject".
            return "That file is not a valid JPG or PNG photo.";
        }

        return "";
    }

    // Call ValidatePhoto first. Saves a 200x200 cropped JPG under a GUID name and returns true.
    // Metadata (EXIF/GPS etc.) is stripped, and EXIF rotation is applied first so phone photos stay upright.
    // Returns false (and leaves no file behind) if the image cannot be processed.
    public bool TrySavePhoto(IFormFile f, string folder, out string file)
    {
        file = "";
        var dir = Path.Combine(en.WebRootPath, folder);
        Directory.CreateDirectory(dir);

        var name = Guid.NewGuid().ToString("n") + ".jpg";
        var path = Path.Combine(dir, name);

        try
        {
            using var stream = f.OpenReadStream();
            using var img = SixLabors.ImageSharp.Image.Load(stream);

            img.Mutate(x => x
                .AutoOrient()
                .Resize(new ResizeOptions { Size = new Size(200, 200), Mode = ResizeMode.Crop })
                .BackgroundColor(Color.White));   // PNG transparency would turn black in a JPG

            img.Metadata.ExifProfile = null;
            img.Metadata.IccProfile = null;
            img.Metadata.IptcProfile = null;
            img.Metadata.XmpProfile = null;

            img.SaveAsJpeg(path, new JpegEncoder { Quality = 85 });
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (File.Exists(path)) File.Delete(path);
            return false;
        }

        file = name;
        return true;
    }

    public void DeletePhoto(string? file, string folder)
    {
        if (string.IsNullOrEmpty(file)) return;

        file = Path.GetFileName(file);   // never allow path segments
        var path = Path.Combine(en.WebRootPath, folder, file);
        if (File.Exists(path)) File.Delete(path);
    }

    // ------------------------------------------------------------------------
    // Email (prints to the console when Smtp:Host is not configured)
    // ------------------------------------------------------------------------

    public void SendEmail(string to, string subject, string body)
    {
        var host = cfg["Smtp:Host"];
        if (string.IsNullOrEmpty(host))
        {
            Console.WriteLine($"[EMAIL] To: {to}\nSubject: {subject}\n{body}");
            return;
        }

        using var client = new SmtpClient(host, cfg.GetValue("Smtp:Port", 587))
        {
            EnableSsl = cfg.GetValue("Smtp:EnableSsl", true)
        };

        var user = cfg["Smtp:User"];
        if (!string.IsNullOrEmpty(user))
        {
            client.Credentials = new NetworkCredential(user, cfg["Smtp:Password"]);
        }

        var from = cfg["Smtp:From"] ?? "no-reply@pawcare.example";
        client.Send(new MailMessage(from, to, subject, body));
    }

    // ------------------------------------------------------------------------
    // Google reCAPTCHA (disabled when the keys are empty)
    // ------------------------------------------------------------------------

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
            return false;   // fail closed if the check cannot complete
        }
    }
}