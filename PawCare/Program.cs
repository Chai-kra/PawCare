using Microsoft.EntityFrameworkCore;
using PawCare.Models;
using Microsoft.AspNetCore.Mvc;
using PawCare.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddSecretsEnv(builder.Environment.ContentRootPath);

builder.Services.AddControllersWithViews(o =>
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddPawCareDatabase(builder.Configuration, builder.Environment);
builder.Services.AddPawCareAuth();
builder.Services.AddPawCareServices();

var app = builder.Build();

app.EnsureDatabaseCreated();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();   // before UseAuthorization
app.UseAuthorization();
app.MapDefaultControllerRoute();

app.Run();