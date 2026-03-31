using CarPoint.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CarPoint.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Car> Cars => Set<Car>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Rental> Rentals => Set<Rental>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<Favorite> Favorites => Set<Favorite>();
        public DbSet<CarPoint.Models.AdminEvent> AdminEvents { get; set; } = default!;
        public DbSet<CarPoint.Models.SupportTicket> SupportTickets { get; set; } = default!;
        public DbSet<CarPoint.Models.SupportTicketMessage> SupportTicketMessages { get; set; } = default!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Car>()
                .Property(c => c.SalePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Car>()
                .Property(c => c.RentPricePerDay)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Rental>()
                .Property(r => r.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Rental>()
                .Property(r => r.PricePerDay)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(s => s.FinalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Favorite>()
                .HasIndex(f => new { f.UserId, f.CarId })
                .IsUnique();

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Car)
                .WithMany()
                .HasForeignKey(f => f.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
