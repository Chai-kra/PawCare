namespace PawCare.Controllers;

public class AccountController(Database db, Helper hp) : Controller
{
    private const int ResetMinutes = 60;      // how long a reset link works
    private const string PhotoFolder = "photos";

    // ------------------------------------------------------------ Register
    // GET: Account/Register
    public IActionResult Register()
    {
        if (User.Identity!.IsAuthenticated) return RedirectToLanding(User.IsInRole(AppRoles.Customer));
        return View();
    }

    // POST: Account/Register
    [HttpPost]
    public IActionResult Register(RegisterVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLower();
        if (EmailTaken(email))
        {
            ModelState.AddModelError("Email", "That email is already registered.");
            return View(vm);
        }

        // Next id: C000001, C000002, ...
        string max = db.Customers.Max(c => c.Id) ?? $"{IdPrefix.Customer}000000";
        int n = int.Parse(max[1..]) + 1;

        var token = hp.RandomToken();
        var m = new Customer
        {
            Id = $"{IdPrefix.Customer}{n:D6}",
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

    // GET: Account/CheckEmail (live duplicate check used by [Remote] on RegisterVM.Email)
    // Returns true when the email is free. Register POST re-checks, so this is only a convenience.
    [AcceptVerbs("GET", "POST")]
    public IActionResult CheckEmail(string? email)
    {
        var address = (email ?? "").Trim().ToLower();
        return Json(!EmailTaken(address));
    }

    // ------------------------------------------------------------ Email verification
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

        if (m == null || !hp.IsTokenValid(m.VerifyTokenHash, m.VerifyTokenExpires, token))
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

    // ------------------------------------------------------------ Login / Logout
    // GET: Account/Login
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity!.IsAuthenticated) return RedirectToLanding(User.IsInRole(AppRoles.Customer));
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // POST: Account/Login (customers and staff/admins share this form)
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

        string? id = null, name = null, role = null, hash = null;
        bool confirmed = true;

        var c = db.Customers.FirstOrDefault(x => x.Email == email);
        if (c != null)
        {
            id = c.Id;
            name = c.Name;
            role = AppRoles.Customer;
            hash = c.PasswordHash;
            confirmed = c.EmailConfirmed;
        }
        else
        {
            var s = db.Staff.FirstOrDefault(x => x.Email == email);

            // Staff IDs must use their own prefix, so NameIdentifier can never equal a customer ID.
            // Only known roles may sign in.
            if (s != null
                && s.Id.StartsWith(IdPrefix.Staff)
                && (s.Role == AppRoles.Staff || s.Role == AppRoles.Admin))
            {
                id = s.Id;
                name = s.Name;
                role = s.Role;
                hash = s.PasswordHash;
            }
        }

        // Always verify (against a dummy hash if there is none) so timing does not reveal which emails exist.
        var passwordOk = hp.VerifyPassword(hash, vm.Password);

        if (!passwordOk || id == null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(vm);
        }

        if (!confirmed)
        {
            ModelState.AddModelError("", "Please verify your email address before logging in.");
            return View(vm);
        }

        hp.SignIn(id, name!, email, role!, vm.RememberMe);

        if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl!);
        return RedirectToLanding(role == AppRoles.Customer);
    }

    // POST: Account/Logout
    [HttpPost]
    public IActionResult Logout()
    {
        hp.SignOut();
        return RedirectToAction("Index", "Home");
    }

    // ------------------------------------------------------------ Forgot / reset password
    // GET: Account/ForgotPassword
    public IActionResult ForgotPassword()
    {
        return View();
    }

    // POST: Account/ForgotPassword
    [HttpPost]
    public IActionResult ForgotPassword(ForgotPasswordVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLower();
        var token = hp.RandomToken();
        var tokenHash = hp.HashToken(token);
        var expires = DateTime.UtcNow.AddMinutes(ResetMinutes);
        string? name = null;

        var c = db.Customers.FirstOrDefault(x => x.Email == email);
        if (c != null)
        {
            if (CanIssueReset(c.ResetTokenExpires))
            {
                c.ResetTokenHash = tokenHash;
                c.ResetTokenExpires = expires;
                name = c.Name;
            }
        }
        else
        {
            var s = db.Staff.FirstOrDefault(x => x.Email == email);
            if (s != null && CanIssueReset(s.ResetTokenExpires))
            {
                s.ResetTokenHash = tokenHash;
                s.ResetTokenExpires = expires;
                name = s.Name;
            }
        }

        if (name != null)
        {
            db.SaveChanges();
            SendPasswordReset(name, email, token);
        }

        // Same message whether or not the account exists, or the email could be sent.
        TempData["Success"] = "If that email belongs to an account, we have sent a password reset link.";
        return RedirectToAction("Login");
    }

    // GET: Account/ResetPassword (opened from the emailed link)
    public IActionResult ResetPassword(string? email, string? token)
    {
        var (c, s) = FindByResetToken(email, token);
        if (c == null && s == null)
        {
            TempData["Error"] = "That reset link is invalid or has expired. Request a new one below.";
            return RedirectToAction("ForgotPassword");
        }

        return View(new ResetPasswordVM { Email = email!, Token = token! });
    }

    // POST: Account/ResetPassword
    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        // The token is checked again here: the GET check proves nothing about this request.
        var (c, s) = FindByResetToken(vm.Email, vm.Token);
        if (c == null && s == null)
        {
            TempData["Error"] = "That reset link is invalid or has expired. Request a new one below.";
            return RedirectToAction("ForgotPassword");
        }

        var hash = hp.HashPassword(vm.Password);

        if (c != null)
        {
            c.PasswordHash = hash;
            c.ResetTokenHash = null;          // single use
            c.ResetTokenExpires = null;
            c.EmailConfirmed = true;          // they just proved they control this mailbox
            c.VerifyTokenHash = null;
            c.VerifyTokenExpires = null;
        }
        else
        {
            s!.PasswordHash = hash;
            s.ResetTokenHash = null;
            s.ResetTokenExpires = null;
        }
        db.SaveChanges();

        TempData["Success"] = "Password updated. You can log in now.";
        return RedirectToAction("Login");
    }

    // ------------------------------------------------------------ Change password (any signed-in account)
    // GET: Account/ChangePassword
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordVM());
    }

    // POST: Account/ChangePassword
    [Authorize]
    [HttpPost]
    public IActionResult ChangePassword(ChangePasswordVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var id = User.AccountId();
        var isCustomer = User.IsInRole(AppRoles.Customer);

        Customer? c = isCustomer ? db.Customers.Find(id) : null;
        Staff? s = isCustomer ? null : db.Staff.Find(id);

        if (c == null && s == null)
        {
            // Account was removed: clear the cookie.
            hp.SignOut();
            return RedirectToAction("Login");
        }

        var currentHash = c != null ? c.PasswordHash : s!.PasswordHash;

        if (!hp.VerifyPassword(currentHash, vm.CurrentPassword))
        {
            ModelState.AddModelError(nameof(vm.CurrentPassword), "Current password is incorrect.");
            return View(vm);
        }

        if (vm.NewPassword == vm.CurrentPassword)
        {
            ModelState.AddModelError(nameof(vm.NewPassword), "New password must be different from the current one.");
            return View(vm);
        }

        var newHash = hp.HashPassword(vm.NewPassword);

        if (c != null)
        {
            c.PasswordHash = newHash;
            c.ResetTokenHash = null;          // an old, still-valid reset link must not outlive the change
            c.ResetTokenExpires = null;
        }
        else
        {
            s!.PasswordHash = newHash;
            s.ResetTokenHash = null;
            s.ResetTokenExpires = null;
        }
        db.SaveChanges();

        TempData["Success"] = "Your password has been changed.";
        return RedirectToLanding(isCustomer);
    }

    // ------------------------------------------------------------ Dashboard / profile (customers)
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

    // GET: Account/UpdateProfile
    [Authorize(Roles = "Customer")]
    public IActionResult UpdateProfile()
    {
        var m = db.Customers.Find(User.CustomerId());
        if (m == null)
        {
            hp.SignOut();
            return RedirectToAction("Login");
        }

        return View(new UpdateProfileVM
        {
            Email = m.Email,
            PhotoURL = m.PhotoURL,
            Name = m.Name,
            Phone = m.Phone,
        });
    }

    // POST: Account/UpdateProfile
    [Authorize(Roles = "Customer")]
    [HttpPost]
    public IActionResult UpdateProfile(UpdateProfileVM vm)
    {
        var m = db.Customers.Find(User.CustomerId());
        if (m == null)
        {
            hp.SignOut();
            return RedirectToAction("Login");
        }

        // Phone: optional, normalised before saving
        string? phone = null;
        if (!hp.TryNormalizePhone(vm.Phone, out phone))
            ModelState.AddModelError(nameof(vm.Phone), "Enter a valid phone number, e.g. +60 12-345 6789.");

        // Photo: optional; type, size and content are all checked
        var hasPhoto = vm.Photo != null && vm.Photo.Length > 0;
        if (hasPhoto)
        {
            var error = hp.ValidatePhoto(vm.Photo!);
            if (error != "") ModelState.AddModelError(nameof(vm.Photo), error);
        }

        // Only Name, Phone and Photo are ever applied. Email and PhotoURL from the form are ignored.
        string? newFile = null;
        if (ModelState.IsValid && hasPhoto && !hp.TrySavePhoto(vm.Photo!, PhotoFolder, out newFile))
            ModelState.AddModelError(nameof(vm.Photo), "That photo could not be processed. Try a different image.");

        if (!ModelState.IsValid)
        {
            vm.Email = m.Email;           // redisplay the real values, not whatever was posted
            vm.PhotoURL = m.PhotoURL;
            return View(vm);
        }

        var oldFile = m.PhotoURL;

        m.Name = vm.Name.Trim();
        m.Phone = phone;
        if (newFile != null) m.PhotoURL = newFile;

        try
        {
            db.SaveChanges();
        }
        catch
        {
            if (newFile != null) hp.DeletePhoto(newFile, PhotoFolder);   // do not leave an orphan
            throw;
        }

        if (newFile != null) hp.DeletePhoto(oldFile, PhotoFolder);       // replace, do not accumulate

        // The header shows the name from the cookie, so refresh it.
        hp.RefreshSignIn(m.Id, m.Name, m.Email, AppRoles.Customer);

        TempData["Success"] = "Profile updated.";
        return RedirectToAction("Index");
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ------------------------------------------------------------ helpers
    // Customers have a dashboard; staff and admins have no dashboard yet, so they go home.
    private IActionResult RedirectToLanding(bool isCustomer) =>
        isCustomer ? RedirectToAction("Index") : RedirectToAction("Index", "Home");

    // An email is taken if either table has it (login searches both).
    private bool EmailTaken(string email) =>
        email != "" && (db.Customers.Any(c => c.Email == email) || db.Staff.Any(s => s.Email == email));

    // Only issue a new reset token if none is pending or the last one is over a minute old.
    private static bool CanIssueReset(DateTime? currentExpiry) =>
        currentExpiry == null || currentExpiry < DateTime.UtcNow.AddMinutes(ResetMinutes - 1);

    // The account (customer or staff) whose reset token matches, or (null, null).
    private (Customer? customer, Staff? staff) FindByResetToken(string? email, string? token)
    {
        var address = (email ?? "").Trim().ToLower();
        if (address == "" || string.IsNullOrEmpty(token)) return (null, null);

        var c = db.Customers.FirstOrDefault(x => x.Email == address);
        if (c != null)
            return hp.IsTokenValid(c.ResetTokenHash, c.ResetTokenExpires, token) ? (c, null) : (null, null);

        var s = db.Staff.FirstOrDefault(x => x.Email == address);
        if (s != null && hp.IsTokenValid(s.ResetTokenHash, s.ResetTokenExpires, token))
            return (null, s);

        return (null, null);
    }

    // Returns false if the email could not be sent.
    private bool SendMail(string to, string subject, string body)
    {
        try
        {
            hp.SendEmail(to, subject, body);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool SendVerification(Customer m, string token)
    {
        var link = hp.PublicUrl(Url.Action("VerifyEmail", "Account", new { email = m.Email, token })!);
        return SendMail(m.Email, "Verify your PawCare email",
            $"Hi {m.Name},\n\nConfirm your email address by opening this link (valid for 24 hours):\n{link}\n\nIf you did not create this account, you can ignore this message.");
    }

    private bool SendPasswordReset(string name, string email, string token)
    {
        var link = hp.PublicUrl(Url.Action("ResetPassword", "Account", new { email, token })!);
        return SendMail(email, "Reset your PawCare password",
            $"Hi {name},\n\nWe received a request to reset your password. Open this link to choose a new one (valid for {ResetMinutes} minutes):\n{link}\n\nIf you did not ask for this, you can ignore this message. Your password will not change.");
    }
}