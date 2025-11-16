using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using TransportationsSystem.Models;

namespace TransportationsSystem.Data
{
    public class MySqlHelper
    {
        private readonly string _conn;
        public MySqlHelper(string conn) { _conn = conn; }

        private IDbConnection Connection => new MySqlConnection(_conn);

        // Users
        public async Task<User> GetUserByUsernameAsync(string username)
        {
            using var db = Connection;
            return await db.QueryFirstOrDefaultAsync<User>("SELECT * FROM users WHERE username=@username LIMIT 1", new { username });
        }

        public async Task<int> CreateUserAsync(User u)
        {
            using var db = Connection;
            string sql = @"INSERT INTO users (full_name, username, email, password_hash, salt, role)
                           VALUES (@full_name,@username,@email,@password_hash,@salt,@role); SELECT LAST_INSERT_ID();";
            return await db.ExecuteScalarAsync<int>(sql, u);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            using var db = Connection;
            return await db.QueryFirstOrDefaultAsync<User>("SELECT * FROM users WHERE id=@id", new { id });
        }

        // ? Get all drivers (users with role = 'DRIVER')
        public async Task<IEnumerable<User>> GetAllDriversAsync()
        {
            using var db = Connection;
            return await db.QueryAsync<User>("SELECT * FROM users WHERE role='DRIVER' ORDER BY full_name");
        }

        // Vehicles
        public async Task<IEnumerable<Vehicle>> GetAllVehiclesAsync()
        {
            using var db = Connection;
            return await db.QueryAsync<Vehicle>("SELECT * FROM vehicles ORDER BY name");
        }

        public async Task<Vehicle> GetVehicleByIdAsync(int id)
        {
            using var db = Connection;
            return await db.QueryFirstOrDefaultAsync<Vehicle>("SELECT * FROM vehicles WHERE id=@id", new { id });
        }

        // Bookings
        public async Task<int> CountBookingsForVehicleAtScheduleAsync(int vehicleId, DateTime schedule)
        {
            using var db = Connection;
            string sql = @"SELECT COUNT(*) FROM bookings
                           WHERE vehicle_id = @vehicleId AND schedule = @schedule AND status IN ('PENDING','APPROVED')";
            return await db.ExecuteScalarAsync<int>(sql, new { vehicleId, schedule });
        }

        public async Task<int> CountApprovedBookingsForVehicleAtScheduleAsync(int vehicleId, DateTime schedule)
        {
            using var db = Connection;
            string sql = @"SELECT COUNT(*) FROM bookings
                           WHERE vehicle_id = @vehicleId AND schedule = @schedule AND status = 'APPROVED'";
            return await db.ExecuteScalarAsync<int>(sql, new { vehicleId, schedule });
        }

        public async Task<int> CreateBookingAsync(Booking b)
        {
            using var db = Connection;
            string sql = @"INSERT INTO bookings 
        (user_id, vehicle_id, vehicle_name, capacity, origin, destination, schedule, status, created_at, 
         trip_type, trip_days, payment_amount, payment_status)
        VALUES (@user_id, @vehicle_id, @vehicle_name, @capacity, @origin, @destination, @schedule, @status, @created_at,
                @trip_type, @trip_days, @payment_amount, @payment_status);
        SELECT LAST_INSERT_ID();";
            return await db.ExecuteScalarAsync<int>(sql, b);
        }


        public async Task<IEnumerable<Booking>> GetBookingsByUserAsync(int userId)
        {
            using var db = Connection;
            string sql = @"
        SELECT b.id, b.user_id, b.vehicle_id, b.origin, b.destination,
               b.schedule, b.status, b.created_at, b.driver_id, b.driver_status,
               b.trip_type, b.trip_days, b.payment_amount, b.payment_status,
               v.name AS vehicle_name, v.capacity,
               d.full_name AS driver_name
        FROM bookings b
        INNER JOIN vehicles v ON b.vehicle_id = v.id
        LEFT JOIN users d ON b.driver_id = d.id
        WHERE b.user_id = @userId
        ORDER BY b.created_at DESC";
            return await db.QueryAsync<Booking>(sql, new { userId });
        }


        public async Task<IEnumerable<Booking>> GetAllBookingsWithVehicleAsync()
        {
            using var db = Connection;
            string sql = @"SELECT b.*, v.name AS vehicle_name, v.capacity,
                           d.full_name AS driver_name
                           FROM bookings b
                           INNER JOIN vehicles v ON b.vehicle_id = v.id
                           LEFT JOIN users d ON b.driver_id = d.id
                           ORDER BY b.created_at DESC";
            return await db.QueryAsync<Booking>(sql);
        }

        public async Task<Booking> GetBookingByIdAsync(int id)
        {
            using var db = Connection;
            return await db.QueryFirstOrDefaultAsync<Booking>("SELECT * FROM bookings WHERE id=@id", new { id });
        }

        public async Task<int> UpdateBookingStatusAsync(int id, string newStatus)
        {
            using var db = Connection;
            string sql = "UPDATE bookings SET status = @newStatus WHERE id = @id";
            return await db.ExecuteAsync(sql, new { newStatus = newStatus.ToUpper(), id });
        }

        // Cancel booking - only if PENDING
        public async Task<bool> CancelBookingAsync(int bookingId, int userId)
        {
            using var db = Connection;
            string sql = @"UPDATE bookings 
  SET status = 'CANCELLED' 
      WHERE id = @bookingId 
  AND user_id = @userId 
       AND status = 'PENDING'";
            var rowsAffected = await db.ExecuteAsync(sql, new { bookingId, userId });
            return rowsAffected > 0;
        }

        // ? Assign driver to booking
        public async Task<bool> AssignDriverToBookingAsync(int bookingId, int driverId)
        {
            using var db = Connection;
            string sql = @"UPDATE bookings 
  SET driver_id = @driverId,
 driver_status = 'PENDING_DRIVER_RESPONSE'
   WHERE id = @bookingId AND status = 'APPROVED'";
            var rowsAffected = await db.ExecuteAsync(sql, new { bookingId, driverId });
            return rowsAffected > 0;
        }

        // ? Get bookings assigned to a specific driver
        public async Task<IEnumerable<Booking>> GetBookingsByDriverAsync(int driverId)
        {
            using var db = Connection;
            string sql = @"
        SELECT b.id, b.user_id, b.vehicle_id, b.origin, b.destination,
               b.schedule, b.status, b.created_at, b.driver_id, b.driver_status,
               b.trip_type, b.trip_days, b.payment_amount, b.payment_status,
               v.name AS vehicle_name, v.capacity,
               u.full_name AS user_full_name
        FROM bookings b
        INNER JOIN vehicles v ON b.vehicle_id = v.id
     LEFT JOIN users u ON b.user_id = u.id
        WHERE b.driver_id = @driverId AND b.status = 'APPROVED'
        ORDER BY b.schedule ASC";
            return await db.QueryAsync<Booking>(sql, new { driverId });
        }

        // ? Driver accepts assignment
        public async Task<bool> AcceptAssignmentAsync(int bookingId, int driverId)
        {
            using var db = Connection;
            string sql = @"UPDATE bookings 
         SET driver_status = 'ACCEPTED' 
  WHERE id = @bookingId 
  AND driver_id = @driverId 
    AND status = 'APPROVED'";
            var rowsAffected = await db.ExecuteAsync(sql, new { bookingId, driverId });
            return rowsAffected > 0;
        }

        // ? Driver declines assignment
        public async Task<bool> DeclineAssignmentAsync(int bookingId, int driverId)
        {
            using var db = Connection;
            string sql = @"UPDATE bookings 
    SET driver_status = 'DECLINED',
     driver_id = NULL
            WHERE id = @bookingId 
      AND driver_id = @driverId 
AND status = 'APPROVED'";
            var rowsAffected = await db.ExecuteAsync(sql, new { bookingId, driverId });
            return rowsAffected > 0;
        }

        // ? Get all users
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
    using var db = Connection;
            return await db.QueryAsync<User>("SELECT * FROM users ORDER BY role, full_name");
        }

      // ? Get dashboard statistics
        public async Task<Dictionary<string, int>> GetDashboardStatsAsync()
        {
          using var db = Connection;
            var stats = new Dictionary<string, int>();

            // Total bookings
     stats["TotalBookings"] = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM bookings");
            
    // Pending bookings
            stats["PendingBookings"] = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM bookings WHERE status='PENDING'");
            
    // Approved bookings
   stats["ApprovedBookings"] = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM bookings WHERE status='APPROVED'");
         
            // Total users
       stats["TotalUsers"] = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE role='USER'");
    
 // Total drivers
            stats["TotalDrivers"] = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE role='DRIVER'");
            
            // Total vehicles
      stats["TotalVehicles"] = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM vehicles");

          return stats;
        }

        // ? Delete user by ID
        public async Task<bool> DeleteUserAsync(int userId)
        {
            using var db = Connection;
        string sql = "DELETE FROM users WHERE id = @userId";
var rowsAffected = await db.ExecuteAsync(sql, new { userId });
 return rowsAffected > 0;
    }

        // ? Update user role
public async Task<bool> UpdateUserRoleAsync(int userId, string newRole)
      {
using var db = Connection;
  string sql = "UPDATE users SET role = @newRole WHERE id = @userId";
    var rowsAffected = await db.ExecuteAsync(sql, new { userId, newRole });
      return rowsAffected > 0;
        }

        // ? Mark booking as paid
        public async Task<bool> MarkBookingAsPaidAsync(int bookingId, int userId)
        {
            using var db = Connection;
            string sql = @"UPDATE bookings 
                          SET payment_status = 'PAID' 
                          WHERE id = @bookingId 
                          AND user_id = @userId 
                          AND payment_status = 'UNPAID'";
            var rowsAffected = await db.ExecuteAsync(sql, new { bookingId, userId });
            return rowsAffected > 0;
        }

        // ? Confirm payment by driver
        public async Task<bool> ConfirmPaymentByDriverAsync(int bookingId, int driverId)
        {
            using var db = Connection;
            string sql = @"UPDATE bookings 
                          SET payment_status = 'PAID' 
                          WHERE id = @bookingId 
                          AND driver_id = @driverId 
                          AND status = 'APPROVED'";
            var rowsAffected = await db.ExecuteAsync(sql, new { bookingId, driverId });
            return rowsAffected > 0;
        }

        // ? Get driver revenue statistics
        public async Task<Dictionary<string, decimal>> GetDriverRevenueStatsAsync(int driverId)
        {
            using var db = Connection;
            var stats = new Dictionary<string, decimal>();

            // Total earnings (all paid bookings)
            var totalEarningsSql = @"SELECT COALESCE(SUM(payment_amount), 0) 
                                    FROM bookings 
                                    WHERE driver_id = @driverId 
                                    AND payment_status = 'PAID' 
                                    AND status = 'APPROVED'";
            stats["TotalEarnings"] = await db.ExecuteScalarAsync<decimal>(totalEarningsSql, new { driverId });

            // Pending earnings (accepted but unpaid)
            var pendingEarningsSql = @"SELECT COALESCE(SUM(payment_amount), 0) 
                                       FROM bookings 
                                       WHERE driver_id = @driverId 
                                       AND payment_status = 'UNPAID' 
                                       AND driver_status = 'ACCEPTED' 
                                       AND status = 'APPROVED'";
            stats["PendingEarnings"] = await db.ExecuteScalarAsync<decimal>(pendingEarningsSql, new { driverId });

            // This month's earnings
            var monthlyEarningsSql = @"SELECT COALESCE(SUM(payment_amount), 0) 
                                       FROM bookings 
                                       WHERE driver_id = @driverId 
                                       AND payment_status = 'PAID' 
                                       AND status = 'APPROVED' 
                                       AND YEAR(schedule) = YEAR(CURDATE()) 
                                       AND MONTH(schedule) = MONTH(CURDATE())";
            stats["MonthlyEarnings"] = await db.ExecuteScalarAsync<decimal>(monthlyEarningsSql, new { driverId });

            // Completed trips count
            var completedTripsSql = @"SELECT COUNT(*) 
                                     FROM bookings 
                                     WHERE driver_id = @driverId 
                                     AND payment_status = 'PAID' 
                                     AND status = 'APPROVED'";
            stats["CompletedTrips"] = await db.ExecuteScalarAsync<decimal>(completedTripsSql, new { driverId });

            return stats;
        }

        // ? Get driver monthly revenue breakdown
        public async Task<IEnumerable<dynamic>> GetDriverMonthlyRevenueAsync(int driverId, int months = 6)
        {
            using var db = Connection;
            string sql = @"SELECT 
                            DATE_FORMAT(schedule, '%Y-%m') AS month,
                            DATE_FORMAT(schedule, '%b %Y') AS month_name,
                            COUNT(*) AS trip_count,
                            SUM(CASE WHEN payment_status = 'PAID' THEN payment_amount ELSE 0 END) AS revenue,
                            SUM(CASE WHEN payment_status = 'UNPAID' THEN payment_amount ELSE 0 END) AS pending
                          FROM bookings 
                          WHERE driver_id = @driverId 
                          AND status = 'APPROVED'
                          AND schedule >= DATE_SUB(CURDATE(), INTERVAL @months MONTH)
                          GROUP BY DATE_FORMAT(schedule, '%Y-%m'), DATE_FORMAT(schedule, '%b %Y')
                          ORDER BY month DESC";
            return await db.QueryAsync(sql, new { driverId, months });
        }
    }
}
