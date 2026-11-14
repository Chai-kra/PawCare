using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PawCare.Models;
using PawCare.Services;

var builder = WebApplication.CreateBuilder(args);

// Validate anti-forgery tokens on all POST requests.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// File-based LocalDB; |DataDirectory| resolves to App_Data.
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("Connection string 'Database' not found.");
builder.Services.AddSqlServer<Database>(connectionString.Replace("|DataDirectory|", dataDirectory));

// Authentication (cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "PawCare.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;   // use Always when serving HTTPS only
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

// Account services
builder.Services.AddSingleton<IPasswordHasher<Customer>, PasswordHasher<Customer>>();
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
builder.Services.AddHttpClient<IRecaptchaService, RecaptchaService>();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();   // before UseAuthorization
app.UseAuthorization();
app.MapDefaultControllerRoute();
app.Run();
