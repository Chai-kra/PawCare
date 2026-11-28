global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
global using System.ComponentModel.DataAnnotations;
global using PawCare;
global using PawCare.Models;

using PawCare.Extensions;
using PawCare.Services;

var builder = WebApplication.CreateBuilder(args);

// Loads PawCare/Secret.env (Smtp__Host=... becomes Smtp:Host).
// Added after the default sources, so it overrides appsettings.
builder.Configuration.AddSecretsEnv(builder.Environment.ContentRootPath);

// Validate anti-forgery tokens on every POST.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// LocalDB file inside App_Data (connection string is in appsettings.json)
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
builder.Services.AddSqlServer<Database>(
    builder.Configuration.GetConnectionString("Database")!.Replace("|DataDirectory|", dataDirectory));

builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<Helper>();
builder.Services.AddAuthentication().AddCookie();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();   // shopping cart (CartController and _Layout)

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultControllerRoute();
app.Run();