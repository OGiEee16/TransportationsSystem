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

        // ✅ Check if payment columns exist, if not add them
        var checkPaymentColumnsSql = @"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_SCHEMA = 'transportationsystem' 
              AND TABLE_NAME = 'bookings' 
              AND COLUMN_NAME IN ('payment_amount', 'payment_status', 'trip_type', 'trip_days')";
        
        using (var checkPaymentCommand = new MySqlCommand(checkPaymentColumnsSql, connection))
        {
            var paymentColumnsCount = Convert.ToInt32(checkPaymentCommand.ExecuteScalar());
            
            if (paymentColumnsCount < 4) // If not all payment columns exist
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Adding payment columns to bookings table...");

                try
                {
                    // Check and add payment_amount column
                    var checkCol1 = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'bookings' 
                                     AND COLUMN_NAME = 'payment_amount'";
                    using (var cmd = new MySqlCommand(checkCol1, connection))
                    {
                        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        {
                            var addPaymentAmountSql = @"ALTER TABLE bookings 
                                ADD COLUMN payment_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00";
                            using var addCmd = new MySqlCommand(addPaymentAmountSql, connection);
                            addCmd.ExecuteNonQuery();
                            logger.LogInformation("✅ Added payment_amount column");
                        }
                    }

                    // Check and add payment_status column
                    var checkCol2 = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'bookings' 
                                     AND COLUMN_NAME = 'payment_status'";
                    using (var cmd = new MySqlCommand(checkCol2, connection))
                    {
                        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        {
                            var addPaymentStatusSql = @"ALTER TABLE bookings 
                                ADD COLUMN payment_status VARCHAR(20) NOT NULL DEFAULT 'UNPAID'";
                            using var addCmd = new MySqlCommand(addPaymentStatusSql, connection);
                            addCmd.ExecuteNonQuery();
                            logger.LogInformation("✅ Added payment_status column");
                        }
                    }

                    // Check and add trip_type column
                    var checkCol3 = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'bookings' 
                                     AND COLUMN_NAME = 'trip_type'";
                    using (var cmd = new MySqlCommand(checkCol3, connection))
                    {
                        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        {
                            var addTripTypeSql = @"ALTER TABLE bookings 
                                ADD COLUMN trip_type VARCHAR(20) NOT NULL DEFAULT 'ONE_WAY'";
                            using var addCmd = new MySqlCommand(addTripTypeSql, connection);
                            addCmd.ExecuteNonQuery();
                            logger.LogInformation("✅ Added trip_type column");
                        }
                    }

                    // Check and add trip_days column
                    var checkCol4 = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                                     WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'bookings' 
                                     AND COLUMN_NAME = 'trip_days'";
                    using (var cmd = new MySqlCommand(checkCol4, connection))
                    {
                        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        {
                            var addTripDaysSql = @"ALTER TABLE bookings 
                                ADD COLUMN trip_days INT NOT NULL DEFAULT 1";
                            using var addCmd = new MySqlCommand(addTripDaysSql, connection);
                            addCmd.ExecuteNonQuery();
                            logger.LogInformation("✅ Added trip_days column");
                        }
                    }

                    // Add indexes if they don't exist
                    try
                    {
                        var checkIndex1 = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
                                           WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'bookings' 
                                           AND INDEX_NAME = 'idx_payment_status'";
                        using (var cmd = new MySqlCommand(checkIndex1, connection))
                        {
                            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                            {
                                var addIndexSql = @"ALTER TABLE bookings ADD INDEX idx_payment_status (payment_status)";
                                using var addCmd = new MySqlCommand(addIndexSql, connection);
                                addCmd.ExecuteNonQuery();
                            }
                        }

                        var checkIndex2 = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
                                           WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'bookings' 
                                           AND INDEX_NAME = 'idx_trip_type'";
                        using (var cmd = new MySqlCommand(checkIndex2, connection))
                        {
                            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                            {
                                var addIndexSql = @"ALTER TABLE bookings ADD INDEX idx_trip_type (trip_type)";
                                using var addCmd = new MySqlCommand(addIndexSql, connection);
                                addCmd.ExecuteNonQuery();
                            }
                        }

                        logger.LogInformation("✅ Added indexes for payment columns");
                    }
                    catch (Exception idxEx)
                    {
                        logger.LogWarning($"Indexes might already exist: {idxEx.Message}");
                    }

                    logger.LogInformation("🎉 Payment system is ready!");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Error adding payment columns. Please run add_payment_columns.sql manually.");
                    logger.LogError("See FIX_MIGRATION_ERROR.md for instructions.");
                }
            }
            else
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("✅ Payment columns already exist.");
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
