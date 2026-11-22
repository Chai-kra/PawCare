using System.Security.Claims;

namespace PawCare.Models;

public static class AppRoles
{
    public const string Customer = "Customer";
    public const string Staff = "Staff";
}

public static class ClaimsPrincipalExtensions
{
    public static string? CustomerId(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}