using System.Security.Claims;

namespace PawCare.Models;

public static class AppRoles
{
    public const string Customer = "Customer";
    public const string Staff = "Staff";
    public const string Admin = "Admin";
}

// Each table has its own ID prefix, so ClaimTypes.NameIdentifier is never ambiguous.
public static class IdPrefix
{
    public const char Customer = 'C';
    public const char Staff = 'S';
}

public static class ClaimsPrincipalExtensions
{
    // Raw ID of whoever is signed in (customer or staff)
    public static string? AccountId(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    // Only returns an ID for customers, so a staff ID can never be used as a customer ID
    public static string? CustomerId(this ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Customer) ? user.AccountId() : null;
}