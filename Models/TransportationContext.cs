using Microsoft.EntityFrameworkCore;
using TransportationsSystem.Models; // make sure you have a Models folder

namespace TransportationsSystem.Data
{
    public class TransportationContext : DbContext
    {
        public TransportationContext(DbContextOptions<TransportationContext> options)
            : base(options)
        {
        }

        // Example tables:
        public DbSet<User> Users { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names to match existing database schema
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Booking>().ToTable("bookings");
            modelBuilder.Entity<Vehicle>().ToTable("vehicles");
        }
    }
}
