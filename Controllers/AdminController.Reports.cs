using System.Globalization;
using EcommerceApp.Helpers;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApp.Controllers
{
    // Sección Reportes del panel de administración.
    public partial class AdminController
    {
        private static readonly string[] MesesCsv =
        {
            "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
            "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
        };

        // Monto en número plano (sin "Bs.", cultura invariante) para que Excel lo trate como número.
        private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

        private static string Money(decimal? amount) => Money(amount ?? 0m);

        private static string MesNombre(int year, int month) => $"{MesesCsv[month]} {year}";

        // Arma el archivo descargable a partir de filas ya armadas (CsvHelper hace el escapado).
        private FileContentResult CsvFile(string fileName, IEnumerable<string[]> rows)
        {
            var bytes = CsvHelper.ToUtf8Bytes(CsvHelper.BuildCsv(rows));
            return File(bytes, "text/csv", fileName);
        }

        // Índice con una tarjeta por reporte.
        public IActionResult Reports()
        {
            ViewData["Title"] = "Reportes";
            ViewData["Subtitle"] = "Ingresos, reservas, ventas, inventario, demanda, usuarios y guías/transportes";
            return View();
        }

        public async Task<IActionResult> ReportIncome([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            ViewData["Title"] = "Ingresos";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetIncomeReportAsync(d, h);
            return View(vm);
        }

        public async Task<IActionResult> ReportIncomeCsv([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            var vm = await reportService.GetIncomeReportAsync(d, h);

            var rows = new List<string[]>
            {
                new[] { "RESUMEN" },
                new[] { "Ingresos por reservas", Money(vm.TotalReservas) },
                new[] { "Ingresos por productos", Money(vm.TotalProductos) },
                new[] { "Total general", Money(vm.TotalGeneral) },
                new[] { "Por confirmar (reservas)", Money(vm.PorConfirmarReservas) },
                new[] { "Por confirmar (pedidos)", Money(vm.PorConfirmarPedidos) },
                new[] { "Por confirmar (total)", Money(vm.PorConfirmarTotal) },
                Array.Empty<string>(),
                new[] { "POR MES" },
                new[] { "Mes", "Reservas", "Productos", "Total" }
            };
            rows.AddRange(vm.PorMes.Select(m => new[] { MesNombre(m.Year, m.Month), Money(m.Reservas), Money(m.Productos), Money(m.Total) }));
            rows.Add(Array.Empty<string>());
            rows.Add(new[] { "POR CONFIRMAR" });
            rows.Add(new[] { "Origen", "Monto" });
            rows.Add(new[] { "Reservas (Pendiente / Recorrido)", Money(vm.PorConfirmarReservas) });
            rows.Add(new[] { "Pedidos (Pendiente)", Money(vm.PorConfirmarPedidos) });

            return CsvFile($"reporte-ingresos_{d:yyyy-MM-dd}_{h:yyyy-MM-dd}.csv", rows);
        }

        public async Task<IActionResult> ReportReservations(
            [FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta, string? estado, int? serviceId)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            ViewData["Title"] = "Reservas";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetReservationsReportAsync(d, h, estado, serviceId);
            return View(vm);
        }

        public async Task<IActionResult> ReportReservationsCsv(
            [FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta, string? estado, int? serviceId)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            var vm = await reportService.GetReservationsReportAsync(d, h, estado, serviceId);

            var rows = new List<string[]>
            {
                new[] { "POR ESTADO" },
                new[] { "Estado", "Cantidad", "Monto" }
            };
            rows.AddRange(vm.PorEstado.Select(e => new[] { e.Status, e.Count.ToString(CultureInfo.InvariantCulture), Money(e.Amount) }));
            rows.Add(Array.Empty<string>());
            rows.Add(new[] { "Tasa de cancelación (%)", vm.TasaCancelacion.ToString("0.0", CultureInfo.InvariantCulture) });
            rows.Add(Array.Empty<string>());
            rows.Add(new[] { "POR RUTA" });
            rows.Add(new[] { "Ruta", "Cantidad", "Personas", "Monto Acabado" });
            rows.AddRange(vm.PorRuta.Select(r => new[] { r.RouteName, r.Count.ToString(CultureInfo.InvariantCulture), r.People.ToString(CultureInfo.InvariantCulture), Money(r.AmountAcabado) }));
            rows.Add(Array.Empty<string>());
            rows.Add(new[] { "POR MES" });
            rows.Add(new[] { "Mes", "Cantidad", "Monto" });
            rows.AddRange(vm.PorMes.Select(m => new[] { MesNombre(m.Year, m.Month), m.Count.ToString(CultureInfo.InvariantCulture), Money(m.Amount) }));

            return CsvFile($"reporte-reservas_{d:yyyy-MM-dd}_{h:yyyy-MM-dd}.csv", rows);
        }

        public async Task<IActionResult> ReportProductSales([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            ViewData["Title"] = "Ventas de productos";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetProductSalesReportAsync(d, h);
            return View(vm);
        }

        public async Task<IActionResult> ReportProductSalesCsv([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            var vm = await reportService.GetProductSalesReportAsync(d, h);

            var rows = new List<string[]>
            {
                new[] { "POR PRODUCTO" },
                new[] { "Producto", "Unidades", "Monto" }
            };
            rows.AddRange(vm.PorProducto.Select(p => new[] { p.ProductName, p.Quantity.ToString(CultureInfo.InvariantCulture), Money(p.Amount) }));
            rows.Add(Array.Empty<string>());
            rows.Add(new[] { "POR CATEGORÍA" });
            rows.Add(new[] { "Categoría", "Unidades", "Monto" });
            rows.AddRange(vm.PorCategoria.Select(c => new[] { c.Category, c.Quantity.ToString(CultureInfo.InvariantCulture), Money(c.Amount) }));

            return CsvFile($"reporte-ventas_{d:yyyy-MM-dd}_{h:yyyy-MM-dd}.csv", rows);
        }

        public async Task<IActionResult> ReportInventory([FromServices] ReportService reportService)
        {
            ViewData["Title"] = "Inventario";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetInventoryReportAsync();
            return View(vm);
        }

        public async Task<IActionResult> ReportInventoryCsv([FromServices] ReportService reportService)
        {
            var vm = await reportService.GetInventoryReportAsync();
            var sinStock = vm.StockBajo.Concat(vm.Agotados).OrderBy(p => p.Stock);

            var rows = new List<string[]>
            {
                new[] { "STOCK BAJO Y AGOTADOS" },
                new[] { "Producto", "Categoría", "Stock", "Estado" }
            };
            rows.AddRange(sinStock.Select(p => new[] { p.Name, p.Category ?? "—", p.Stock.ToString(CultureInfo.InvariantCulture), p.Stock == 0 ? "Agotado" : "Stock bajo" }));
            rows.Add(Array.Empty<string>());
            rows.Add(new[] { "POR CATEGORÍA" });
            rows.Add(new[] { "Categoría", "Productos" });
            rows.AddRange(vm.PorCategoria.Select(c => new[] { c.Category, c.Count.ToString(CultureInfo.InvariantCulture) }));
            rows.Add(Array.Empty<string>());
            rows.Add(new[] { "POR MARCA" });
            rows.Add(new[] { "Marca", "Productos" });
            rows.AddRange(vm.PorMarca.Select(b => new[] { b.Brand, b.Count.ToString(CultureInfo.InvariantCulture) }));
            rows.Add(Array.Empty<string>());
            rows.Add(new[] { "EN OFERTA" });
            rows.Add(new[] { "Producto", "Precio normal", "Precio de oferta", "Stock" });
            rows.AddRange(vm.EnOferta.Select(p => new[] { p.Name, Money(p.Price), Money(p.PromotionalPrice), p.Stock.ToString(CultureInfo.InvariantCulture) }));

            return CsvFile($"reporte-inventario_{DateTime.UtcNow:yyyy-MM-dd}.csv", rows);
        }

        public async Task<IActionResult> ReportDemand([FromServices] ReportService reportService)
        {
            ViewData["Title"] = "Demanda";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetDemandReportAsync();
            return View(vm);
        }

        public async Task<IActionResult> ReportDemandCsv([FromServices] ReportService reportService)
        {
            var vm = await reportService.GetDemandReportAsync();

            var rows = new List<string[]>
            {
                new[] { "PRODUCTOS FAVORITOS" },
                new[] { "Producto", "Veces guardado" }
            };
            rows.AddRange(vm.ProductosFavoritos.Select(p => new[] { p.Nombre, p.Cantidad.ToString(CultureInfo.InvariantCulture) }));
            rows.Add(Array.Empty<string>());
            rows.Add(new[] { "RUTAS FAVORITAS" });
            rows.Add(new[] { "Ruta", "Veces guardada" });
            rows.AddRange(vm.RutasFavoritas.Select(r => new[] { r.Nombre, r.Cantidad.ToString(CultureInfo.InvariantCulture) }));
            rows.Add(Array.Empty<string>());
            rows.Add(new[] { "PRODUCTOS EN CARRITOS" });
            rows.Add(new[] { "Producto", "Cantidad total", "Carritos distintos" });
            rows.AddRange(vm.ProductosEnCarrito.Select(c => new[] { c.ProductName, c.CantidadTotal.ToString(CultureInfo.InvariantCulture), c.CarritosDistintos.ToString(CultureInfo.InvariantCulture) }));

            return CsvFile($"reporte-demanda_{DateTime.UtcNow:yyyy-MM-dd}.csv", rows);
        }

        public async Task<IActionResult> ReportUsers([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            ViewData["Title"] = "Usuarios";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetUsersReportAsync(d, h);
            return View(vm);
        }

        public async Task<IActionResult> ReportUsersCsv([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            var vm = await reportService.GetUsersReportAsync(d, h);

            var rows = new List<string[]>
            {
                new[] { "Total registrados", vm.TotalRegistrados.ToString(CultureInfo.InvariantCulture) },
                Array.Empty<string>(),
                new[] { "POR MES" },
                new[] { "Mes", "Registros" }
            };
            rows.AddRange(vm.PorMes.Select(m => new[] { MesNombre(m.Year, m.Month), m.Cantidad.ToString(CultureInfo.InvariantCulture) }));

            return CsvFile($"reporte-usuarios_{d:yyyy-MM-dd}_{h:yyyy-MM-dd}.csv", rows);
        }

        public async Task<IActionResult> ReportGuidesTransport([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            ViewData["Title"] = "Guías y transportes";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetGuidesTransportReportAsync(d, h);
            return View(vm);
        }

        public async Task<IActionResult> ReportGuidesTransportCsv([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            var vm = await reportService.GetGuidesTransportReportAsync(d, h);

            var rows = new List<string[]>
            {
                new[] { "RUTAS POR GUÍA" },
                new[] { "Guía", "Rutas asignadas" }
            };
            rows.AddRange(vm.RutasPorGuia.Select(g => new[] { g.GuideName, g.CantidadRutas.ToString(CultureInfo.InvariantCulture) }));
            rows.Add(Array.Empty<string>());
            rows.Add(new[] { "RUTAS POR TRANSPORTE" });
            rows.Add(new[] { "Transporte", "Rutas asignadas" });
            rows.AddRange(vm.RutasPorTransporte.Select(t => new[] { t.TransportName, t.CantidadRutas.ToString(CultureInfo.InvariantCulture) }));
            rows.Add(Array.Empty<string>());
            rows.Add(new[] { "RESERVAS ACABADO POR GUÍA" });
            rows.Add(new[] { "Guía", "Cantidad", "Personas", "Monto" });
            rows.AddRange(vm.ReservasPorGuia.Select(r => new[] { r.GuideName, r.Cantidad.ToString(CultureInfo.InvariantCulture), r.Personas.ToString(CultureInfo.InvariantCulture), Money(r.Monto) }));

            return CsvFile($"reporte-guias-transportes_{d:yyyy-MM-dd}_{h:yyyy-MM-dd}.csv", rows);
        }
    }
}
