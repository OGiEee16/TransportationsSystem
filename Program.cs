using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TransportationsSystem.Data;
using MySql.Data.MySqlClient;

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

// ✅ Initialize database - create vehicles table if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    try
    {
      using var connection = new MySqlConnection(conn);
        connection.Open();

        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS vehicles (
             id INT AUTO_INCREMENT PRIMARY KEY,
     name VARCHAR(255) NOT NULL,
      type VARCHAR(100) NOT NULL,
           capacity INT NOT NULL
        ) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
        ";

        using (var command = new MySqlCommand(createTableSql, connection))
        {
     command.ExecuteNonQuery();
        }

  // ✅ Check if driver_id column exists, if not add it
        var checkColumnSql = @"
            SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'transportationsystem' 
     AND TABLE_NAME = 'bookings' 
   AND COLUMN_NAME = 'driver_id'";
  
        using (var checkCommand = new MySqlCommand(checkColumnSql, connection))
  {
       var columnExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
        
            if (!columnExists)
      {
     var addDriverColumnSql = @"
               ALTER TABLE bookings 
            ADD COLUMN driver_id INT NULL,
          ADD INDEX idx_driver_id (driver_id)";
    
        using var alterCommand = new MySqlCommand(addDriverColumnSql, connection);
            alterCommand.ExecuteNonQuery();
    
       var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
         logger.LogInformation("Successfully added driver_id column to bookings table.");
            }
        }

   // ✅ Check if driver_status column exists, if not add it
   var checkDriverStatusSql = @"
     SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
       WHERE TABLE_SCHEMA = 'transportationsystem' 
         AND TABLE_NAME = 'bookings' 
    AND COLUMN_NAME = 'driver_status'";
        
  using (var checkStatusCommand = new MySqlCommand(checkDriverStatusSql, connection))
        {
    var statusColumnExists = Convert.ToInt32(checkStatusCommand.ExecuteScalar()) > 0;
            
     if (!statusColumnExists)
  {
 var addDriverStatusSql = @"
            ALTER TABLE bookings 
     ADD COLUMN driver_status VARCHAR(50) NULL DEFAULT 'PENDING_DRIVER_RESPONSE'";
        
    using var alterStatusCommand = new MySqlCommand(addDriverStatusSql, connection);
   alterStatusCommand.ExecuteNonQuery();
    
       var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Successfully added driver_status column to bookings table.");
     }
    }

     // Check if there are any vehicles, if not, seed some data
        using (var checkCommand = new MySqlCommand("SELECT COUNT(*) FROM vehicles", connection))
 {
    var count = Convert.ToInt32(checkCommand.ExecuteScalar());
       if (count == 0)
            {
 var insertDataSql = @"
     INSERT INTO vehicles (name, type, capacity) VALUES
        ('City Bus 101', 'Bus', 50),
       ('Van 202', 'Van', 15),
        ('Shuttle 303', 'Shuttle', 25),
         ('Coach 404', 'Coach', 40);";
  
 using var insertCommand = new MySqlCommand(insertDataSql, connection);
  insertCommand.ExecuteNonQuery();
            }
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

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
