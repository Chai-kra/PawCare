using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using PawCare.Models;
using PawCare.Services;

namespace PawCare.Extensions;

public static class ServExt
{
    public static IServiceCollection AddPawCareDatabase(
        this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dir);

        var cs = config.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' not found.");

        return services.AddSqlServer<Database>(cs.Replace("|DataDirectory|", dir));
    }

    public static IServiceCollection AddPawCareAuth(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.Cookie.Name = "PawCare.Auth";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Always when HTTPS-only
                o.LoginPath = "/Account/Login";
                o.AccessDeniedPath = "/Account/AccessDenied";
                o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                o.SlidingExpiration = true;
            });

        return services.AddAuthorization();
    }

    public static IServiceCollection AddPawCareServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher<Customer>, PasswordHasher<Customer>>();
        services.AddSingleton<IEmailService, SmtpEmailService>();
        services.AddHttpClient<IRecaptchaService, RecaptchaService>();
        return services;
    }

    public static void EnsureDatabaseCreated(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<Database>().Database.EnsureCreated();
    }
}