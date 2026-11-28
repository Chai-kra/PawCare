global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
global using System.ComponentModel.DataAnnotations;
global using PawCare;
global using PawCare.Models;

using PawCare.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddSecretsEnv(builder.Environment.ContentRootPath);

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
builder.Services.AddSqlServer<Database>(
    builder.Configuration.GetConnectionString("Database")!.Replace("|DataDirectory|", dataDirectory));

builder.Services.AddScoped<Helper>();
builder.Services.AddAuthentication().AddCookie();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultControllerRoute();
app.Run();