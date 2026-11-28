#nullable disable warnings

namespace PawCare.Models;

// View Models ----------------------------------------------------------------
// Properties are non-nullable, so MVC already treats them as required.

// One place for rules shared by Register, Change password and Reset password.
public static class Rules
{
    public const string UsernamePattern = @"^[A-Za-z0-9 _.\-]+$";
    public const string UsernameMessage = "Use letters, numbers, spaces, . _ or - only.";

    public const string PasswordLengthMessage = "Password must be 8 to 100 characters.";
    public const string PasswordPattern = @"^(?=.*[A-Za-z])(?=.*\d).+$";
    public const string PasswordMessage = "Password needs at least one letter and one number.";
}

public class RegisterVM
{
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(Rules.UsernamePattern, ErrorMessage = Rules.UsernameMessage)]
    public string Username { get; set; }

    [StringLength(100)]
    [EmailAddress]
    [Remote("CheckEmail", "Account", ErrorMessage = "That email is already registered.")]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 8, ErrorMessage = Rules.PasswordLengthMessage)]
    [RegularExpression(Rules.PasswordPattern, ErrorMessage = Rules.PasswordMessage)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; }
}

public class LoginVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}

public class ResendVerificationVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }
}

public class ForgotPasswordVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }
}

// Email + Token come from the link and travel as hidden fields; both are re-checked on POST.
public class ResetPasswordVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(128)]
    public string Token { get; set; }

    [StringLength(100, MinimumLength = 8, ErrorMessage = Rules.PasswordLengthMessage)]
    [RegularExpression(Rules.PasswordPattern, ErrorMessage = Rules.PasswordMessage)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string Password { get; set; }

    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; }
}

public class ChangePasswordVM
{
    [StringLength(100)]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; }

    [StringLength(100, MinimumLength = 8, ErrorMessage = Rules.PasswordLengthMessage)]
    [RegularExpression(Rules.PasswordPattern, ErrorMessage = Rules.PasswordMessage)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; }

    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; }
}

// Edit model for the profile page. The Customer entity is never bound.
// Email and PhotoURL are for display only: the POST action ignores them.
public class UpdateProfileVM
{
    public string? Email { get; set; }
    public string? PhotoURL { get; set; }

    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(Rules.UsernamePattern, ErrorMessage = Rules.UsernameMessage)]
    [Display(Name = "Username")]
    public string Name { get; set; }

    [StringLength(20, ErrorMessage = "Phone number is too long.")]
    [RegularExpression(@"^[+0-9 \-]*$", ErrorMessage = "Use digits, spaces, - and a leading + only.")]
    public string? Phone { get; set; }

    public IFormFile? Photo { get; set; }
}