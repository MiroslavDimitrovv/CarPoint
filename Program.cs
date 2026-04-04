using System.Globalization;
using CarPoint.Data;
using CarPoint.Models;
using CarPoint.Services.CarValuation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")?.Trim();

if (!string.IsNullOrWhiteSpace(connectionString) &&
    (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
     connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
{
    connectionString = ConvertPostgresUrlToNpgsql(connectionString);
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
        connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString);
        return;
    }

    options.UseSqlServer(connectionString);
});

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddHttpClient<AutoDevCarValuationService>();
builder.Services.AddScoped<ICarValuationService, AutoDevCarValuationService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CarPoint.Services.AdminEvents.IAdminEventLogger, CarPoint.Services.AdminEvents.AdminEventLogger>();
builder.Services.AddScoped<CarPoint.Services.SupportNotifications.ISupportNotificationService, CarPoint.Services.SupportNotifications.SupportNotificationService>();
builder.Services.AddSession();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var hasMigrations = db.Database.GetMigrations().Any();
    var isPostgres = db.Database.IsNpgsql();
    if (app.Environment.IsDevelopment() || !hasMigrations || isPostgres)
    {
        db.Database.EnsureCreated();
    }
    else
    {
        db.Database.Migrate();
    }
}

await AppSeed.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var supportedCultures = new[] { new CultureInfo("bg-BG") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("bg-BG"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

static string ConvertPostgresUrlToNpgsql(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfoParts = uri.UserInfo.Split(':', 2);
    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.Trim('/'),
        Username = Uri.UnescapeDataString(userInfoParts[0]),
        Password = userInfoParts.Length > 1 ? Uri.UnescapeDataString(userInfoParts[1]) : string.Empty,
        SslMode = SslMode.Require
    };

    return builder.ConnectionString;
}
