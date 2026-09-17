using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Models;

namespace EcommerceApp.Data
{
    // Constructor primario: reemplaza el constructor clásico + base(options)
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Guide> Guides { get; set; }
        public DbSet<Transport> Transports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.PromotionalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Service>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);

            // Clave compuesta (UserId, ServiceId) + relaciones de las reservas.
            modelBuilder.Entity<Reservation>(r =>
            {
                r.HasKey(x => new { x.UserId, x.ServiceId });
                r.Property(x => x.UnitPrice).HasPrecision(18, 2);
                r.Property(x => x.TotalPrice).HasPrecision(18, 2);

                r.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                r.HasOne(x => x.Service)
                    .WithMany()
                    .HasForeignKey(x => x.ServiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Transport>()
                .Property(t => t.PricePerKm)
                .HasPrecision(18, 2);
        }
    }
}
