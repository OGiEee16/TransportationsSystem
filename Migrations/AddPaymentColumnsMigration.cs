using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace TransportationsSystem.Migrations
{
    /// <summary>
    /// Database migration tool to add payment columns
    /// Run this once to update your database schema
    /// </summary>
    public class AddPaymentColumnsMigration
    {
        private readonly string _connectionString;

        public AddPaymentColumnsMigration(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> ExecuteAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                Console.WriteLine("?? Starting payment columns migration...");

                // Check if columns already exist
                if (await ColumnExists(connection, "payment_amount"))
                {
                    Console.WriteLine("? Payment columns already exist. Skipping migration.");
                    return true;
                }

                // Add payment_amount column
                await ExecuteSqlAsync(connection, @"
                    ALTER TABLE bookings 
                    ADD COLUMN payment_amount DECIMAL(10,2) NOT NULL DEFAULT 0.00 
                    AFTER driver_status;
                ");
                Console.WriteLine("? Added payment_amount column");

                // Add payment_status column
                await ExecuteSqlAsync(connection, @"
                    ALTER TABLE bookings 
                    ADD COLUMN payment_status VARCHAR(20) NOT NULL DEFAULT 'UNPAID' 
                    AFTER payment_amount;
                ");
                Console.WriteLine("? Added payment_status column");

                // Add trip_type column
                await ExecuteSqlAsync(connection, @"
                    ALTER TABLE bookings 
                    ADD COLUMN trip_type VARCHAR(20) NOT NULL DEFAULT 'ONE_WAY' 
                    AFTER payment_status;
                ");
                Console.WriteLine("? Added trip_type column");

                // Add trip_days column
                await ExecuteSqlAsync(connection, @"
                    ALTER TABLE bookings 
                    ADD COLUMN trip_days INT NOT NULL DEFAULT 1 
                    AFTER trip_type;
                ");
                Console.WriteLine("? Added trip_days column");

                // Add indexes
                await ExecuteSqlAsync(connection, @"
                    ALTER TABLE bookings 
                    ADD INDEX idx_payment_status (payment_status);
                ");
                Console.WriteLine("? Added payment_status index");

                await ExecuteSqlAsync(connection, @"
                    ALTER TABLE bookings 
                    ADD INDEX idx_trip_type (trip_type);
                ");
                Console.WriteLine("? Added trip_type index");

                Console.WriteLine("?? Payment columns migration completed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Migration failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ColumnExists(MySqlConnection connection, string columnName)
        {
            using var cmd = new MySqlCommand(@"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_SCHEMA = DATABASE() 
                  AND TABLE_NAME = 'bookings' 
                  AND COLUMN_NAME = @columnName
            ", connection);
            
            cmd.Parameters.AddWithValue("@columnName", columnName);
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        private async Task ExecuteSqlAsync(MySqlConnection connection, string sql)
        {
            using var cmd = new MySqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Console application to run the migration
    /// Usage: dotnet run --project MigrationRunner
    /// </summary>
    public class MigrationRunner
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Payment System Database Migration");
            Console.WriteLine("========================================");
            Console.WriteLine();

            try
            {
                // Load configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("? Connection string not found in appsettings.json");
                    return;
                }

                Console.WriteLine($"?? Database: {GetDatabaseName(connectionString)}");
                Console.WriteLine();
                Console.WriteLine("Press any key to start migration...");
                Console.ReadKey();
                Console.WriteLine();

                var migration = new AddPaymentColumnsMigration(connectionString);
                var success = await migration.ExecuteAsync();

                Console.WriteLine();
                if (success)
                {
                    Console.WriteLine("? Migration completed successfully!");
                    Console.WriteLine("You can now run your application.");
                }
                else
                {
                    Console.WriteLine("? Migration failed. Please check the errors above.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Unexpected error: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static string GetDatabaseName(string connectionString)
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            return builder.Database;
        }
    }
}
