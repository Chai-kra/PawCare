using PawCare.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

// SQL Server Express / LocalDB, file-based. "|DataDirectory|" -> <project>/App_Data
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("Connection string 'Database' not found.");
builder.Services.AddSqlServer<Database>(connectionString.Replace("|DataDirectory|", dataDirectory));

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapDefaultControllerRoute();
app.Run();
