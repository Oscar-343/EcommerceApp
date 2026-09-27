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
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<ProveedorMarca> ProveedorMarcas { get; set; }
        public DbSet<Abastecimiento> Abastecimientos { get; set; }
        public DbSet<DetalleAbastecimiento> DetallesAbastecimiento { get; set; }

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

            // PK propia por Id. Único (UserId, ServiceId, TripDate): mismo usuario y ruta
            // pueden reservar en fechas distintas, pero no dos veces la misma fecha.
            modelBuilder.Entity<Reservation>(r =>
            {
                r.HasKey(x => x.Id);
                r.Property(x => x.UnitPrice).HasPrecision(18, 2);
                r.Property(x => x.TotalPrice).HasPrecision(18, 2);

                r.HasIndex(x => new { x.UserId, x.ServiceId, x.TripDate }).IsUnique();

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

            // ---------------- PROVEEDORES Y ABASTECIMIENTOS ----------------

            // Nombre de marca único. Restrict: una marca con productos no se puede borrar
            // (igual la app solo desactiva marcas).
            modelBuilder.Entity<Marca>(m =>
            {
                m.HasIndex(x => x.Nombre).IsUnique();

                m.HasMany(x => x.Productos)
                    .WithOne(p => p.Marca)
                    .HasForeignKey(p => p.MarcaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Proveedor>()
                .HasIndex(p => p.Nombre);

            // Clave compuesta (ProveedorId, MarcaId): relación muchos a muchos.
            // Cascade: si se borra el proveedor o la marca, solo desaparece el vínculo.
            modelBuilder.Entity<ProveedorMarca>(pm =>
            {
                pm.HasKey(x => new { x.ProveedorId, x.MarcaId });

                pm.HasOne(x => x.Proveedor)
                    .WithMany(p => p.Marcas)
                    .HasForeignKey(x => x.ProveedorId)
                    .OnDelete(DeleteBehavior.Cascade);

                pm.HasOne(x => x.Marca)
                    .WithMany(m => m.Proveedores)
                    .HasForeignKey(x => x.MarcaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Restrict: un proveedor con abastecimientos no se puede borrar (se desactiva).
            modelBuilder.Entity<Abastecimiento>(a =>
            {
                a.Property(x => x.Total).HasPrecision(18, 2);

                a.HasOne(x => x.Proveedor)
                    .WithMany(p => p.Abastecimientos)
                    .HasForeignKey(x => x.ProveedorId)
                    .OnDelete(DeleteBehavior.Restrict);

                a.HasIndex(x => x.Fecha);
            });

            // Igual que OrderItem: si se borra el producto, la línea conserva su nombre (ProductoId en null).
            modelBuilder.Entity<DetalleAbastecimiento>(d =>
            {
                d.Property(x => x.CostoUnitario).HasPrecision(18, 2);
                d.Property(x => x.Subtotal).HasPrecision(18, 2);

                d.HasOne(x => x.Abastecimiento)
                    .WithMany(a => a.Detalles)
                    .HasForeignKey(x => x.AbastecimientoId)
                    .OnDelete(DeleteBehavior.Cascade);

                d.HasOne(x => x.Producto)
                    .WithMany()
                    .HasForeignKey(x => x.ProductoId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
