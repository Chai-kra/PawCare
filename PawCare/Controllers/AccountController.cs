namespace PawCare.Controllers;

public class AccountController(Database db, Helper hp) : Controller
{
    // GET: Account/Register
    public IActionResult Register()
    {
        if (User.Identity!.IsAuthenticated) return RedirectToAction("Index");
        return View();
    }

    // POST: Account/Register
    [HttpPost]
    public IActionResult Register(RegisterVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLower();
        if (db.Customers.Any(c => c.Email == email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
            return View(vm);
        }

        // Next id: C000001, C000002, ...
        string max = db.Customers.Max(c => c.Id) ?? "C000000";
        int n = int.Parse(max[1..]) + 1;

        var token = hp.RandomToken();
        var m = new Customer
        {
            Id = $"C{n:D6}",
            Name = vm.Username.Trim(),
            Email = email,
            PasswordHash = hp.HashPassword(vm.Password),
            VerifyTokenHash = hp.HashToken(token),
            VerifyTokenExpires = DateTime.UtcNow.AddHours(24),
        };
        db.Customers.Add(m);
        db.SaveChanges();

        TempData["Success"] = SendVerification(m, token)
            ? "Account created. Check your email for the verification link."
            : "Account created, but we could not send the verification email. Use \"Resend verification email\" below.";
        return RedirectToAction("Login");
    }

    // GET: Account/VerifyEmail
    public IActionResult VerifyEmail(string? email, string? token)
    {
        var address = (email ?? "").Trim().ToLower();
        var m = db.Customers.FirstOrDefault(c => c.Email == address);

        if (m != null && m.EmailConfirmed)
        {
            TempData["Success"] = "Your email is already verified. You can log in.";
            return RedirectToAction("Login");
        }

        if (m == null ||
            m.VerifyTokenHash == null ||
            m.VerifyTokenExpires < DateTime.UtcNow ||
            m.VerifyTokenHash != hp.HashToken(token ?? ""))
        {
            TempData["Error"] = "That verification link is invalid or has expired. Request a new one below.";
            return RedirectToAction("ResendVerification");
        }

        m.EmailConfirmed = true;
        m.VerifyTokenHash = null;
        m.VerifyTokenExpires = null;
        db.SaveChanges();

        TempData["Success"] = "Email verified. You can log in now.";
        return RedirectToAction("Login");
    }

    // GET: Account/ResendVerification
    public IActionResult ResendVerification()
    {
        return View();
    }

    // POST: Account/ResendVerification
    [HttpPost]
    public IActionResult ResendVerification(ResendVerificationVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLower();
        var m = db.Customers.FirstOrDefault(c => c.Email == email);

        if (m != null && !m.EmailConfirmed)
        {
            var token = hp.RandomToken();
            m.VerifyTokenHash = hp.HashToken(token);
            m.VerifyTokenExpires = DateTime.UtcNow.AddHours(24);
            db.SaveChanges();
            SendVerification(m, token);
        }

        // Same message either way, so accounts cannot be discovered.
        TempData["Success"] = "If that email belongs to an unverified account, we have sent a new verification link.";
        return RedirectToAction("Login");
    }

    // GET: Account/Login
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity!.IsAuthenticated) return RedirectToAction("Index");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(vm);

        if (!hp.VerifyRecaptcha(Request.Form["g-recaptcha-response"]))
        {
            ModelState.AddModelError("", "Please confirm you are not a robot.");
            return View(vm);
        }

        var email = vm.Email.Trim().ToLower();
        var m = db.Customers.FirstOrDefault(c => c.Email == email);

        if (m == null || !hp.VerifyPassword(m.PasswordHash, vm.Password))
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(vm);
        }

        if (!m.EmailConfirmed)
        {
            ModelState.AddModelError("", "Please verify your email address before logging in.");
            return View(vm);
        }

        hp.SignIn(m.Id, m.Name, m.Email, "Customer", vm.RememberMe);

        if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl!);
        return RedirectToAction("Index");
    }

    // POST: Account/Logout
    [HttpPost]
    public IActionResult Logout()
    {
        hp.SignOut();
        return RedirectToAction("Index", "Home");
    }

    // GET: Account/Index (customer dashboard)
    [Authorize(Roles = "Customer")]
    public IActionResult Index()
    {
        var m = db.Customers.Find(User.CustomerId());

        if (m == null)
        {
            // Account was removed: clear the cookie.
            hp.SignOut();
            return RedirectToAction("Login");
        }

        return View(m);
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied()
    {
        return View();
    }

    // Returns false if the email could not be sent.
    private bool SendVerification(Customer m, string token)
    {
        var link = Url.Action("VerifyEmail", "Account", new { email = m.Email, token }, Request.Scheme);
        try
        {
            hp.SendEmail(m.Email, "Verify your PawCare email",
                $"Hi {m.Name},\n\nConfirm your email address by opening this link (valid for 24 hours):\n{link}\n\nIf you did not create this account, you can ignore this message.");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
