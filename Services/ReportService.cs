using EcommerceApp.Data;
using EcommerceApp.Models.Reports;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Services
{
    // Calcula los reportes del panel admin (Ingresos, Reservas, Ventas de productos, Inventario)
    // a partir de los datos ya existentes (Order/OrderItem/Reservation/Product/Service). Solo lectura.
    public class ReportService(ApplicationDbContext context)
    {
        // Bolivia es UTC-4 sin horario de verano; se resta a mano en vez de usar TimeZoneInfo
        // (mismo criterio que ya usa ReservationsController para "desde mañana").
        private const int BoliviaOffsetHours = -4;

        // Si no vienen fechas, usa el mes en curso (hora Bolivia).
        public static (DateOnly desde, DateOnly hasta) DefaultRange(DateOnly? desde, DateOnly? hasta)
        {
            if (desde.HasValue && hasta.HasValue) return (desde.Value, hasta.Value);

            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(BoliviaOffsetHours));
            var primerDia = new DateOnly(hoy.Year, hoy.Month, 1);
            var ultimoDia = primerDia.AddMonths(1).AddDays(-1);
            return (desde ?? primerDia, hasta ?? ultimoDia);
        }

        // Convierte un rango de fechas locales (Bolivia) inclusive a un rango UTC [desde, hasta) para
        // filtrar columnas DateTime; el fin de rango se trata como exclusivo (hasta + 1 día).
        private static (DateTime desdeUtc, DateTime hastaUtcExclusivo) RangeUtc(DateOnly desde, DateOnly hastaInclusive)
        {
            var desdeLocal = desde.ToDateTime(TimeOnly.MinValue);
            var hastaLocalExclusivo = hastaInclusive.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var desdeUtc = DateTime.SpecifyKind(desdeLocal.AddHours(-BoliviaOffsetHours), DateTimeKind.Utc);
            var hastaUtc = DateTime.SpecifyKind(hastaLocalExclusivo.AddHours(-BoliviaOffsetHours), DateTimeKind.Utc);
            return (desdeUtc, hastaUtc);
        }

        private static DateTime ToLocal(DateTime utc) => utc.AddHours(BoliviaOffsetHours);

        // ---------------------------------------------------------------
        // INGRESOS
        // ---------------------------------------------------------------
        public async Task<IncomeReportViewModel> GetIncomeReportAsync(DateOnly desde, DateOnly hasta)
        {
            var (desdeUtc, hastaUtc) = RangeUtc(desde, hasta);

            var reservasQuery = context.Reservations.AsNoTracking()
                .Where(r => r.Status == "Acabado" && r.CompletedAt != null
                    && r.CompletedAt >= desdeUtc && r.CompletedAt < hastaUtc);
            var pedidosQuery = context.Orders.AsNoTracking()
                .Where(o => o.Status == "Entregado" && o.DeliveredAt != null
                    && o.DeliveredAt >= desdeUtc && o.DeliveredAt < hastaUtc);

            var totalReservas = await reservasQuery.SumAsync(r => (decimal?)r.TotalPrice) ?? 0m;
            var totalProductos = await pedidosQuery.SumAsync(o => (decimal?)o.Total) ?? 0m;

            // Para agrupar por mes en hora Bolivia hay que traer los datos y agrupar en memoria.
            var reservasDetalle = await reservasQuery.Select(r => new { Fecha = r.CompletedAt!.Value, Monto = r.TotalPrice }).ToListAsync();
            var pedidosDetalle = await pedidosQuery.Select(o => new { Fecha = o.DeliveredAt!.Value, Monto = o.Total }).ToListAsync();

            var porMes = reservasDetalle
                .Select(r => new { Fecha = ToLocal(r.Fecha), r.Monto, EsReserva = true })
                .Concat(pedidosDetalle.Select(o => new { Fecha = ToLocal(o.Fecha), o.Monto, EsReserva = false }))
                .GroupBy(x => new { x.Fecha.Year, x.Fecha.Month })
                .Select(g => new IncomeMonthRow
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Reservas = g.Where(x => x.EsReserva).Sum(x => x.Monto),
                    Productos = g.Where(x => !x.EsReserva).Sum(x => x.Monto)
                })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToList();

            var porConfirmarReservas = await context.Reservations.AsNoTracking()
                .Where(r => r.Status == "Pendiente" || r.Status == "Recorrido")
                .SumAsync(r => (decimal?)r.TotalPrice) ?? 0m;
            var porConfirmarPedidos = await context.Orders.AsNoTracking()
                .Where(o => o.Status == "Pendiente")
                .SumAsync(o => (decimal?)o.Total) ?? 0m;

            return new IncomeReportViewModel
            {
                Desde = desde,
                Hasta = hasta,
                TotalReservas = totalReservas,
                TotalProductos = totalProductos,
                PorMes = porMes,
                PorConfirmarReservas = porConfirmarReservas,
                PorConfirmarPedidos = porConfirmarPedidos
            };
        }

        // ---------------------------------------------------------------
        // RESERVAS
        // ---------------------------------------------------------------
        public async Task<ReservationsReportViewModel> GetReservationsReportAsync(
            DateOnly desde, DateOnly hasta, string? estado, int? serviceId)
        {
            var query = context.Reservations.AsNoTracking()
                .Where(r => r.TripDate >= desde && r.TripDate <= hasta);

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(r => r.Status == estado);
            if (serviceId.HasValue)
                query = query.Where(r => r.ServiceId == serviceId.Value);

            var detalle = await query
                .Select(r => new
                {
                    r.Status,
                    r.ServiceId,
                    ServiceName = r.Service != null ? r.Service.Name : "—",
                    r.PeopleCount,
                    r.TotalPrice,
                    r.TripDate
                })
                .ToListAsync();

            var porEstado = detalle
                .GroupBy(r => r.Status)
                .Select(g => new ReservationStatusRow { Status = g.Key, Count = g.Count(), Amount = g.Sum(x => x.TotalPrice) })
                .OrderBy(r => r.Status)
                .ToList();

            var total = detalle.Count;
            var canceladas = detalle.Count(r => r.Status == "Cancelado");
            var tasaCancelacion = total > 0 ? (double)canceladas / total * 100 : 0;

            var porRuta = detalle
                .GroupBy(r => new { r.ServiceId, r.ServiceName })
                .Select(g => new ReservationRouteRow
                {
                    RouteName = g.Key.ServiceName,
                    Count = g.Count(),
                    People = g.Sum(x => x.PeopleCount),
                    AmountAcabado = g.Where(x => x.Status == "Acabado").Sum(x => x.TotalPrice)
                })
                .OrderByDescending(r => r.Count)
                .ToList();

            var porMes = detalle
                .GroupBy(r => new { r.TripDate.Year, r.TripDate.Month })
                .Select(g => new ReservationMonthRow { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count(), Amount = g.Sum(x => x.TotalPrice) })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToList();

            var rutas = await context.Services.AsNoTracking()
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.Name)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            return new ReservationsReportViewModel
            {
                Desde = desde,
                Hasta = hasta,
                Estado = estado,
                ServiceId = serviceId,
                Rutas = rutas.Select(s => (s.Id, s.Name)).ToList(),
                PorEstado = porEstado,
                TasaCancelacion = tasaCancelacion,
                PorRuta = porRuta,
                PorMes = porMes
            };
        }

        // ---------------------------------------------------------------
        // VENTAS DE PRODUCTOS
        // ---------------------------------------------------------------
        public async Task<ProductSalesReportViewModel> GetProductSalesReportAsync(DateOnly desde, DateOnly hasta)
        {
            var (desdeUtc, hastaUtc) = RangeUtc(desde, hasta);

            var query = context.OrderItems.AsNoTracking()
                .Where(oi => oi.Order!.Status == "Entregado" && oi.Order.DeliveredAt != null
                    && oi.Order.DeliveredAt >= desdeUtc && oi.Order.DeliveredAt < hastaUtc);

            var porProducto = await query
                .GroupBy(oi => oi.ProductName)
                .Select(g => new ProductSalesRow
                {
                    ProductName = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Amount = g.Sum(x => x.UnitPrice * x.Quantity)
                })
                .OrderByDescending(r => r.Amount)
                .ToListAsync();

            var porCategoria = await query
                .GroupBy(oi => oi.ProductCategory ?? "Sin categoría")
                .Select(g => new CategorySalesRow
                {
                    Category = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Amount = g.Sum(x => x.UnitPrice * x.Quantity)
                })
                .OrderByDescending(r => r.Amount)
                .ToListAsync();

            return new ProductSalesReportViewModel
            {
                Desde = desde,
                Hasta = hasta,
                PorProducto = porProducto,
                PorCategoria = porCategoria,
                TotalUnidades = porProducto.Sum(r => r.Quantity),
                TotalMonto = porProducto.Sum(r => r.Amount)
            };
        }

        // ---------------------------------------------------------------
        // INVENTARIO (sin filtro de fecha)
        // ---------------------------------------------------------------
        public async Task<InventoryReportViewModel> GetInventoryReportAsync()
        {
            var stockBajo = await context.Products.AsNoTracking()
                .Where(p => p.Stock > 0 && p.Stock <= 5)
                .OrderBy(p => p.Stock)
                .ToListAsync();

            var agotados = await context.Products.AsNoTracking()
                .Where(p => p.Stock == 0)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var valorInventario = await context.Products.AsNoTracking()
                .SumAsync(p => (decimal?)(p.Stock * (p.PromotionalPrice ?? p.Price))) ?? 0m;

            var porCategoria = await context.Products.AsNoTracking()
                .GroupBy(p => p.Category ?? "Sin categoría")
                .Select(g => new CategoryCountRow { Category = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .ToListAsync();

            var porMarca = await context.Products.AsNoTracking()
                .GroupBy(p => p.Brand ?? "Sin marca")
                .Select(g => new BrandCountRow { Brand = g.Key, Count = g.Count() })
                .OrderByDescending(r => r.Count)
                .ToListAsync();

            var enOferta = await context.Products.AsNoTracking()
                .Where(p => p.PromotionalPrice != null)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return new InventoryReportViewModel
            {
                StockBajo = stockBajo,
                Agotados = agotados,
                ValorInventario = valorInventario,
                PorCategoria = porCategoria,
                PorMarca = porMarca,
                EnOferta = enOferta
            };
        }
    }
}
