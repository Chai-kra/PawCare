using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawCare.Models;
using PawCare.Services;
using PawCare.ViewModels;

namespace PawCare.Controllers;

public class AccountController(Database db, IPasswordHasher<Customer> hasher, IEmailService mailer, IRecaptchaService recaptcha) : Controller
{
    // Keeps response time similar whether or not the email exists.
    private static readonly string DummyHash = new PasswordHasher<Customer>().HashPassword(new Customer(), "dummy-Password1");
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

    // ---------- Register ----------
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Index));
        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var address = vm.Email.Trim().ToLowerInvariant();
        if (await db.Customers.AnyAsync(c => c.Email == address))
        {
            ModelState.AddModelError(nameof(vm.Email), "This email is already registered.");
            return View(vm);
        }

        var customer = new Customer
        {
            Id = await NextCustomerIdAsync(),
            Name = vm.Username.Trim(),
            Email = address
        };
        customer.PasswordHash = hasher.HashPassword(customer, vm.Password);
        var token = IssueVerificationToken(customer);

        db.Customers.Add(customer);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "We could not create your account. Please try again.");
            return View(vm);
        }

        var sent = await SendVerificationEmailAsync(customer, token);
        TempData["Success"] = sent
            ? "Account created. Check your email for the verification link."
            : "Account created, but we could not send the verification email. Use \"Resend verification email\" below.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- Email verification ----------
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string? email, string? token)
    {
        var address = (email ?? "").Trim().ToLowerInvariant();
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Email == address);

        if (customer is { EmailConfirmed: true })
        {
            TempData["Success"] = "Your email is already verified. You can log in.";
            return RedirectToAction(nameof(Login));
        }

        var valid = customer is not null
            && !string.IsNullOrEmpty(token)
            && customer.VerifyTokenHash is not null
            && customer.VerifyTokenExpires > DateTime.UtcNow
            && CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(HashToken(token)),
                   Encoding.UTF8.GetBytes(customer.VerifyTokenHash));

        if (!valid)
        {
            TempData["Error"] = "That verification link is invalid or has expired. Request a new one below.";
            return RedirectToAction(nameof(ResendVerification));
        }

        customer!.EmailConfirmed = true;
        customer.VerifyTokenHash = null;
        customer.VerifyTokenExpires = null;
        await db.SaveChangesAsync();

        TempData["Success"] = "Email verified. You can log in now.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResendVerification() => View(new ResendVerificationViewModel());

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> ResendVerification(ResendVerificationViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var address = vm.Email.Trim().ToLowerInvariant();
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Email == address);

        if (customer is { EmailConfirmed: false } && !IssuedRecently(customer))
        {
            var token = IssueVerificationToken(customer);
            await db.SaveChangesAsync();
            await SendVerificationEmailAsync(customer, token);
        }

        // Same response either way, so accounts can't be discovered.
        TempData["Success"] = "If that email belongs to an unverified account, we have sent a new verification link.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- Login ----------
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Index));
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(vm);

        if (!await recaptcha.VerifyAsync(Request.Form["g-recaptcha-response"]))
        {
            ModelState.AddModelError(string.Empty, "Please confirm you are not a robot.");
            return View(vm);
        }

        var address = vm.Email.Trim().ToLowerInvariant();
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Email == address);
        // INSERT (lockout): track failed attempts here.

        var result = hasher.VerifyHashedPassword(customer ?? new Customer(), customer?.PasswordHash ?? DummyHash, vm.Password);
        if (customer is null || result == PasswordVerificationResult.Failed)
        {
            // Generic message: don't reveal which emails exist.
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(vm);
        }

        // Checked after the password so unverified status isn't leaked.
        if (!customer.EmailConfirmed)
        {
            ModelState.AddModelError(string.Empty, "Please verify your email address before logging in.");
            return View(vm);
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            customer.PasswordHash = hasher.HashPassword(customer, vm.Password);
            await db.SaveChangesAsync();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, customer.Id),
            new(ClaimTypes.Name, customer.Name),
            new(ClaimTypes.Email, customer.Email),
            new(ClaimTypes.Role, AppRoles.Customer)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        var properties = new AuthenticationProperties { IsPersistent = vm.RememberMe };
        if (vm.RememberMe) properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);

        // Local URLs only (prevents open redirects).
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    // ---------- Logout (POST only) ----------
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // ---------- Customer dashboard ----------
    [Authorize(Roles = AppRoles.Customer)]
    public async Task<IActionResult> Index()
    {
        var id = User.CustomerId();
        var vm = await db.Customers.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new AccountDashboardViewModel(c.Name, c.Email))
            .FirstOrDefaultAsync();

        if (vm is null)
        {
            // Account was removed: clear the cookie.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
        return View(vm);
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ---------- helpers ----------
    // The raw token goes in the email; only its hash is stored.
    private static string IssueVerificationToken(Customer customer)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        customer.VerifyTokenHash = HashToken(token);
        customer.VerifyTokenExpires = DateTime.UtcNow.Add(TokenLifetime);
        return token;
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    // True if a token was issued within the resend cooldown.
    private static bool IssuedRecently(Customer customer) =>
        customer.VerifyTokenExpires is { } expires && expires > DateTime.UtcNow + (TokenLifetime - ResendCooldown);

    private async Task<bool> SendVerificationEmailAsync(Customer customer, string token)
    {
        var link = Url.Action(nameof(VerifyEmail), "Account", new { email = customer.Email, token }, Request.Scheme);
        try
        {
            await mailer.SendAsync(
                customer.Email,
                "Verify your PawCare email",
                $"Hi {customer.Name},\n\nConfirm your email address by opening this link (valid for 24 hours):\n{link}\n\nIf you did not create this account, you can ignore this message.");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // Ids look like C000001 (fits the 7-character key).
    private async Task<string> NextCustomerIdAsync()
    {
        var last = await db.Customers.OrderByDescending(c => c.Id).Select(c => c.Id).FirstOrDefaultAsync();
        var number = last is null ? 0 : int.Parse(last[1..]);
        return $"C{number + 1:D6}";
    }
}
