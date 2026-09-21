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
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<FavoriteItem> FavoriteItems { get; set; }
        public DbSet<ServiceProduct> ServiceProducts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

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

            // Clave compuesta (ServiceId, ProductId): equipamiento recomendado por ruta.
            // Cascade: si se borra la ruta o el producto, desaparece el vínculo (no el otro lado).
            modelBuilder.Entity<ServiceProduct>(sp =>
            {
                sp.HasKey(x => new { x.ServiceId, x.ProductId });

                sp.HasOne(x => x.Service)
                    .WithMany()
                    .HasForeignKey(x => x.ServiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                sp.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Un pedido no puede tener dos líneas para el mismo producto ni quedar
            // huérfano si se borra el usuario; el usuario nunca se borra desde la app.
            modelBuilder.Entity<Order>(o =>
            {
                o.Property(x => x.Total).HasPrecision(18, 2);

                o.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Snapshot del producto al momento de comprar: si el producto se borra,
            // la línea del pedido conserva su nombre (ProductId queda en null).
            modelBuilder.Entity<OrderItem>(oi =>
            {
                oi.Property(x => x.UnitPrice).HasPrecision(18, 2);

                oi.HasOne(x => x.Order)
                    .WithMany(x => x.Items)
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                oi.HasOne(x => x.Product)
                    .WithMany()
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Una sola línea de carrito por usuario y producto (Add ya evita duplicados en código;
            // esto lo garantiza también a nivel de base de datos).
            modelBuilder.Entity<CartItem>()
                .HasIndex(c => new { c.UserId, c.ProductId })
                .IsUnique();
        }
    }
}
