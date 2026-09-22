using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Models;

namespace EcommerceApp.Data
{
    /// <summary>
    /// Seeder de pedidos, reservas, guías y transportes de prueba.
    /// Ejecutar una sola vez para poblar la base de datos y poder probar los reportes.
    /// </summary>
    public static class OrderReservationSeeder
    {
        public static async Task SeedOrdersAndReservationsAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            var products = await context.Products.ToListAsync();
            var services = await context.Services.ToListAsync();
            if (products.Count == 0 || services.Count == 0)
                return;

            // === Usuarios de prueba (clientes) ===
            var clienteDatos = new[]
            {
                ("cliente1@test.com", "María Fernández"),
                ("cliente2@test.com", "Jorge Quispe"),
                ("cliente3@test.com", "Ana Rodríguez"),
                ("cliente4@test.com", "Luis Mamani"),
                ("cliente5@test.com", "Carla Vargas")
            };

            var clientes = new List<ApplicationUser>();
            foreach (var (email, nombre) in clienteDatos)
            {
                var existente = await userManager.FindByEmailAsync(email);
                if (existente != null)
                {
                    clientes.Add(existente);
                    continue;
                }

                var nuevo = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = nombre,
                    CreatedAt = DateTime.UtcNow.AddMonths(-Random.Shared.Next(1, 6))
                };
                var resultado = await userManager.CreateAsync(nuevo, "Cliente123!");
                if (resultado.Succeeded)
                {
                    await userManager.AddToRoleAsync(nuevo, "User");
                    clientes.Add(nuevo);
                }
            }

            if (clientes.Count == 0)
                return;

            // Idempotente por bloque: usamos los IDs de los clientes de prueba en vez de
            // context.Orders.Any()/context.Reservations.Any() porque la base ya puede tener
            // pedidos/reservas reales de otros usuarios.
            var clienteIds = clientes.Select(c => c.Id).ToHashSet();
            var pedidosYaExisten = context.Orders.Any(o => clienteIds.Contains(o.UserId));
            var reservasYaExisten = context.Reservations.Any(r => clienteIds.Contains(r.UserId));

            // === Guías y transportes ===
            if (!context.Guides.Any())
            {
                context.Guides.AddRange(new List<Guide>
                {
                    new Guide { Name = "Carlos Mamani", Specialty = "Trekking y alta montaña", Bio = "Guía certificado con 10 años de experiencia en rutas de altura.", Phone = "70011122", Email = "carlos.mamani@trenalsur.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddMonths(-6) },
                    new Guide { Name = "Fernando Quispe", Specialty = "Senderismo", Bio = "Especialista en caminatas de naturaleza y grupos familiares.", Phone = "70022233", Email = "fernando.quispe@trenalsur.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddMonths(-5) },
                    new Guide { Name = "Lucía Torrez", Specialty = "Trekking con camping", Bio = "Guía de montaña especializada en expediciones de varios días.", Phone = "70033344", Email = "lucia.torrez@trenalsur.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddMonths(-4) }
                });
                await context.SaveChangesAsync();
            }

            if (!context.Transports.Any())
            {
                context.Transports.AddRange(new List<Transport>
                {
                    new Transport { Name = "Minivan Andina 1", Type = "Minivan", Capacity = 12, Description = "Minivan para grupos pequeños con aire acondicionado.", PricePerKm = 3.5m, IsActive = true, CreatedAt = DateTime.UtcNow.AddMonths(-6) },
                    new Transport { Name = "4x4 Expedición", Type = "4x4", Capacity = 6, Description = "Vehículo todo terreno para rutas de difícil acceso.", PricePerKm = 5m, IsActive = true, CreatedAt = DateTime.UtcNow.AddMonths(-5) },
                    new Transport { Name = "Bus Turístico Sur", Type = "Bus", Capacity = 30, Description = "Bus para grupos grandes en rutas de larga distancia.", PricePerKm = 2.5m, IsActive = true, CreatedAt = DateTime.UtcNow.AddMonths(-4) }
                });
                await context.SaveChangesAsync();
            }

            var guias = await context.Guides.ToListAsync();
            var transportes = await context.Transports.ToListAsync();

            // Asignar guía/transporte a las primeras rutas activas que aún no tengan uno asignado
            var rutasParaAsignar = services.Where(s => s.GuideId == null || s.TransportId == null).Take(5).ToList();
            for (int i = 0; i < rutasParaAsignar.Count; i++)
            {
                rutasParaAsignar[i].GuideId ??= guias[i % guias.Count].Id;
                rutasParaAsignar[i].TransportId ??= transportes[i % transportes.Count].Id;
            }
            if (rutasParaAsignar.Count > 0)
                await context.SaveChangesAsync();

            // === 10 Pedidos variados ===
            var hoy = DateTime.UtcNow;

            if (!pedidosYaExisten)
            {
            var pedidos = new List<Order>();

            (string status, int mesesAtras, bool entregado)[] plantillaPedidos = new[]
            {
                ("Entregado", 3, true),
                ("Entregado", 2, true),
                ("Entregado", 2, true),
                ("Entregado", 1, true),
                ("Entregado", 0, true),
                ("Pendiente", 0, false),
                ("Pendiente", 0, false),
                ("Pendiente", 1, false),
                ("Cancelado", 1, false),
                ("Cancelado", 2, false)
            };

            for (int i = 0; i < plantillaPedidos.Length; i++)
            {
                var (status, mesesAtras, entregado) = plantillaPedidos[i];
                var cliente = clientes[i % clientes.Count];
                var creado = hoy.AddMonths(-mesesAtras).AddDays(-Random.Shared.Next(0, 20));

                var cantidadItems = Random.Shared.Next(1, 4);
                var productosElegidos = products.OrderBy(_ => Random.Shared.Next()).Take(cantidadItems).ToList();

                var items = productosElegidos.Select(p => new OrderItem
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    ProductCategory = p.Category,
                    UnitPrice = p.Price,
                    Quantity = Random.Shared.Next(1, 3)
                }).ToList();

                var order = new Order
                {
                    UserId = cliente.Id,
                    CreatedAt = creado,
                    Status = status,
                    Total = items.Sum(it => it.UnitPrice * it.Quantity),
                    DeliveredAt = entregado ? creado.AddDays(Random.Shared.Next(1, 5)) : null,
                    Items = items
                };
                pedidos.Add(order);
            }

            context.Orders.AddRange(pedidos);
            await context.SaveChangesAsync();
            }

            // === 5 Reservas variadas ===
            if (!reservasYaExisten)
            {
            var rutasConGuia = services.Where(s => s.GuideId != null && s.TransportId != null).ToList();
            if (rutasConGuia.Count == 0)
                rutasConGuia = services;

            (string status, int mesesOffset, bool acabado)[] plantillaReservas = new[]
            {
                ("Acabado", -2, true),
                ("Acabado", -1, true),
                ("Recorrido", 0, false),
                ("Pendiente", 1, false),
                ("Cancelado", 1, false)
            };

            var reservas = new List<Reservation>();
            for (int i = 0; i < plantillaReservas.Length; i++)
            {
                var (status, mesesOffset, acabado) = plantillaReservas[i];
                var cliente = clientes[(i + 1) % clientes.Count];
                var servicio = rutasConGuia[i % rutasConGuia.Count];
                var tripDate = DateOnly.FromDateTime(hoy.AddMonths(mesesOffset).AddDays(Random.Shared.Next(1, 20)));
                var personas = Random.Shared.Next(1, 5);

                var reserva = new Reservation
                {
                    UserId = cliente.Id,
                    ServiceId = servicio.Id,
                    TripDate = tripDate,
                    BookingDate = hoy.AddMonths(mesesOffset).AddDays(-Random.Shared.Next(1, 10)),
                    PeopleCount = personas,
                    UnitPrice = servicio.Price,
                    TotalPrice = servicio.Price * personas,
                    Status = status,
                    CompletedAt = acabado ? DateTime.SpecifyKind(tripDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc) : null
                };
                reservas.Add(reserva);
            }

            context.Reservations.AddRange(reservas);
            await context.SaveChangesAsync();
            }
        }
    }
}
