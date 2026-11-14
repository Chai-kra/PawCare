using System.ComponentModel.DataAnnotations;

namespace PawCare.ViewModels;

public class RegisterViewModel
{
    [Required, StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9 _.\-]+$", ErrorMessage = "Use letters, numbers, spaces, . _ or - only.")]
    public string Username { get; set; } = "";

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be 8 to 100 characters.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Password needs at least one letter and one number.")]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}

public class ResendVerificationViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";
}

// Page model; excludes sensitive fields.
public record AccountDashboardViewModel(string Username, string Email);
