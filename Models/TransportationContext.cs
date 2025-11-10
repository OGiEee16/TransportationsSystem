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
    }
}
