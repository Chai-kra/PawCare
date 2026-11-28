#nullable disable warnings

namespace PawCare.Models;

// View Models ----------------------------------------------------------------
// Properties are non-nullable, so MVC already treats them as required.

public class RegisterVM
{
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9 _.\-]+$", ErrorMessage = "Use letters, numbers, spaces, . _ or - only.")]
    public string Username { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be 8 to 100 characters.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Password needs at least one letter and one number.")]
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
