using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TransportationsSystem.Data;

var builder = WebApplication.CreateBuilder(args);

// Register framework services
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// ✅ Add session services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add DbContext (Pomelo MySQL provider required)
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TransportationContext>(options =>
    options.UseMySql(conn, ServerVersion.AutoDetect(conn)));

// Register cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Optional: register MySqlHelper for DI
// builder.Services.AddScoped(sp => new MySqlHelper(conn));

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// ✅ Enable session middleware (must be before Authentication & Authorization)
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.MapControllers();
app.MapRazorPages();

app.Run();
