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
        (user_id, vehicle_id, vehicle_name, capacity, origin, destination, schedule, status)
        VALUES (@user_id, @vehicle_id, @vehicle_name, @capacity, @origin, @destination, @schedule, @status);
        SELECT LAST_INSERT_ID();";
            return await db.ExecuteScalarAsync<int>(sql, b);
        }


        public async Task<IEnumerable<Booking>> GetBookingsByUserAsync(int userId)
        {
            using var db = Connection;
            string sql = @"
        SELECT b.id, b.user_id, b.vehicle_id, b.origin, b.destination,
               b.schedule, b.status, b.created_at,
               v.name AS vehicle_name, v.capacity
        FROM bookings b
        INNER JOIN vehicles v ON b.vehicle_id = v.id
        WHERE b.user_id = @userId
        ORDER BY b.created_at DESC";
            return await db.QueryAsync<Booking>(sql, new { userId });
        }


        public async Task<IEnumerable<Booking>> GetAllBookingsWithVehicleAsync()
        {
            using var db = Connection;
            string sql = @"SELECT b.*, v.name AS vehicle_name, v.capacity
                           FROM bookings b
                           INNER JOIN vehicles v ON b.vehicle_id = v.id
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
    }
}
