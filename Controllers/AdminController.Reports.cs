using System.Globalization;
using EcommerceApp.Helpers;
using EcommerceApp.Models.Reports;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        private static string MesNombre(int year, int month) => $"{MesesCsv[month]} {year}";

        // ---------------------------------------------------------------
        // EXPORTACIÓN CON DISEÑO (Excel y PDF) — una sola plantilla para todos los reportes.
        // Cada reporte arma un ReporteDocumento; ReporteExcel y ReportePdf lo dibujan.
        // ---------------------------------------------------------------

        // Logo de la barra de navegación (Supabase). Se descarga una vez y queda en memoria.
        private const string LogoUrl = "https://viaymveoxuafmtqhfvri.supabase.co/storage/v1/object/public/product-image/logo.jpg";
        private static byte[]? _logoCache;

        private static async Task<byte[]?> LogoReporteAsync(IHttpClientFactory httpFactory, IWebHostEnvironment env)
        {
            if (_logoCache != null) return _logoCache;

            try
            {
                var http = httpFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(6);
                _logoCache = await http.GetByteArrayAsync(LogoUrl);
                return _logoCache;
            }
            catch (Exception)
            {
                // Sin conexión a Supabase: se usa una copia local si existe (el reporte sale igual).
                foreach (var ruta in new[] { "images/logo-reporte.png", "images/logo-reporte.jpg", "icons/icon-192.png" })
                {
                    var archivo = Path.Combine(env.WebRootPath, ruta);
                    if (System.IO.File.Exists(archivo)) return System.IO.File.ReadAllBytes(archivo);
                }
                return null;
            }
        }

        // Devuelve el reporte en el formato pedido: "excel" (.xlsx) o "pdf" (por defecto).
        private async Task<IActionResult> Exportar(string? formato, string nombreArchivo, ReporteDocumento doc)
        {
            var logo = await LogoReporteAsync(
                HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>(),
                HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>());

            if (string.Equals(formato, "excel", StringComparison.OrdinalIgnoreCase))
                return File(ReporteExcel.Generar(doc, logo),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo + ".xlsx");

            return File(ReportePdf.Generar(doc, logo), "application/pdf", nombreArchivo + ".pdf");
        }

        private static string Periodo(DateOnly d, DateOnly h) => $"{d:dd/MM/yyyy} al {h:dd/MM/yyyy}";

        private static string Hoy() => DateTime.UtcNow.AddHours(-4).ToString("dd/MM/yyyy");

        // Índice con una tarjeta por reporte.
        public IActionResult Reports()
        {
            ViewData["Title"] = "Reportes";
            ViewData["Subtitle"] = "Ingresos, reservas, ventas, inventario, demanda, usuarios y guías/transportes";
            return View();
        }

        // =================================================================
        // INGRESOS
        // =================================================================
        public async Task<IActionResult> ReportIncome([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            ViewData["Title"] = "Ingresos";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetIncomeReportAsync(d, h);
            return View(vm);
        }

        public async Task<IActionResult> ReportIncomeExport([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta, string? formato)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            var vm = await reportService.GetIncomeReportAsync(d, h);

            var porMes = new ReporteTabla
            {
                Titulo = "Ingresos por mes",
                Columnas = new[] { "Mes", "Reservas", "Productos", "Total" },
                ColumnasMoneda = new() { 1, 2, 3 },
                UltimaFilaEsTotal = true
            };
            porMes.Filas.AddRange(vm.PorMes.Select(m => new object?[] { MesNombre(m.Year, m.Month), m.Reservas, m.Productos, m.Total }));
            porMes.Filas.Add(new object?[] { "Total", vm.TotalReservas, vm.TotalProductos, vm.TotalGeneral });

            var doc = new ReporteDocumento
            {
                Titulo = "Reporte de ingresos",
                Descripcion = "Solo cuentan reservas Acabado y pedidos Entregado, por su fecha de finalización.",
                Periodo = Periodo(d, h),
                Resumen =
                {
                    new("Ingresos por reservas", PriceFormatter.Format(vm.TotalReservas)),
                    new("Ingresos por productos", PriceFormatter.Format(vm.TotalProductos)),
                    new("Total general", PriceFormatter.Format(vm.TotalGeneral))
                },
                Tablas =
                {
                    porMes,
                    new ReporteTabla
                    {
                        Titulo = "Por confirmar (todavía no cuenta como ingreso)",
                        Columnas = new[] { "Origen", "Monto" },
                        ColumnasMoneda = new() { 1 },
                        UltimaFilaEsTotal = true,
                        Filas =
                        {
                            new object?[] { "Reservas (Pendiente / Recorrido)", vm.PorConfirmarReservas },
                            new object?[] { "Pedidos (Pendiente)", vm.PorConfirmarPedidos },
                            new object?[] { "Total por confirmar", vm.PorConfirmarTotal }
                        }
                    }
                }
            };
            return await Exportar(formato, $"reporte-ingresos_{d:yyyy-MM-dd}_{h:yyyy-MM-dd}", doc);
        }

        // =================================================================
        // RESERVAS
        // =================================================================
        public async Task<IActionResult> ReportReservations(
            [FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta, string? estado, int? serviceId)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            ViewData["Title"] = "Reservas";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetReservationsReportAsync(d, h, estado, serviceId);
            return View(vm);
        }

        public async Task<IActionResult> ReportReservationsExport(
            [FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta, string? estado, int? serviceId, string? formato)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            var vm = await reportService.GetReservationsReportAsync(d, h, estado, serviceId);

            var filtros = new List<string>();
            if (!string.IsNullOrEmpty(estado)) filtros.Add($"estado {estado}");
            if (serviceId.HasValue)
            {
                var ruta = vm.Rutas.FirstOrDefault(r => r.Id == serviceId.Value).Name;
                if (!string.IsNullOrEmpty(ruta)) filtros.Add($"ruta {ruta}");
            }

            var porEstado = new ReporteTabla { Titulo = "Reservas por estado", Columnas = new[] { "Estado", "Cantidad", "Monto" }, ColumnasMoneda = new() { 2 }, UltimaFilaEsTotal = true };
            porEstado.Filas.AddRange(vm.PorEstado.Select(e => new object?[] { e.Status, e.Count, e.Amount }));
            porEstado.Filas.Add(new object?[] { "Total", vm.PorEstado.Sum(e => e.Count), vm.PorEstado.Sum(e => e.Amount) });

            var porRuta = new ReporteTabla { Titulo = "Reservas por ruta", Columnas = new[] { "Ruta", "Reservas", "Personas", "Monto (Acabado)" }, ColumnasMoneda = new() { 3 } };
            porRuta.Filas.AddRange(vm.PorRuta.Select(r => new object?[] { r.RouteName, r.Count, r.People, r.AmountAcabado }));

            var porMes = new ReporteTabla { Titulo = "Reservas por mes", Columnas = new[] { "Mes", "Reservas", "Monto" }, ColumnasMoneda = new() { 2 } };
            porMes.Filas.AddRange(vm.PorMes.Select(m => new object?[] { MesNombre(m.Year, m.Month), m.Count, m.Amount }));

            var doc = new ReporteDocumento
            {
                Titulo = "Reporte de reservas",
                Descripcion = "Reservas de rutas por fecha de salida" + (filtros.Count > 0 ? " · Filtro: " + string.Join(", ", filtros) : "") + ".",
                Periodo = Periodo(d, h),
                Resumen =
                {
                    new("Reservas", vm.PorEstado.Sum(e => e.Count).ToString("N0", CultureInfo.InvariantCulture)),
                    new("Personas", vm.PorRuta.Sum(r => r.People).ToString("N0", CultureInfo.InvariantCulture)),
                    new("Tasa de cancelación", vm.TasaCancelacion.ToString("0.0", CultureInfo.InvariantCulture) + " %")
                },
                Tablas = { porEstado, porRuta, porMes }
            };
            return await Exportar(formato, $"reporte-reservas_{d:yyyy-MM-dd}_{h:yyyy-MM-dd}", doc);
        }

        // =================================================================
        // VENTAS DE PRODUCTOS
        // =================================================================
        public async Task<IActionResult> ReportProductSales([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            ViewData["Title"] = "Ventas de productos";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetProductSalesReportAsync(d, h);
            return View(vm);
        }

        public async Task<IActionResult> ReportProductSalesExport([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta, string? formato)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            var vm = await reportService.GetProductSalesReportAsync(d, h);

            var porProducto = new ReporteTabla { Titulo = "Productos más vendidos", Columnas = new[] { "Producto", "Unidades", "Monto" }, ColumnasMoneda = new() { 2 }, UltimaFilaEsTotal = true };
            porProducto.Filas.AddRange(vm.PorProducto.Select(p => new object?[] { p.ProductName, p.Quantity, p.Amount }));
            porProducto.Filas.Add(new object?[] { "Total", vm.TotalUnidades, vm.TotalMonto });

            var porCategoria = new ReporteTabla { Titulo = "Ventas por categoría", Columnas = new[] { "Categoría", "Unidades", "Monto" }, ColumnasMoneda = new() { 2 } };
            porCategoria.Filas.AddRange(vm.PorCategoria.Select(c => new object?[] { c.Category, c.Quantity, c.Amount }));

            var doc = new ReporteDocumento
            {
                Titulo = "Reporte de ventas",
                Descripcion = "Productos vendidos en pedidos Entregado, ordenados de más a menos vendido.",
                Periodo = Periodo(d, h),
                Resumen =
                {
                    new("Unidades vendidas", vm.TotalUnidades.ToString("N0", CultureInfo.InvariantCulture)),
                    new("Monto vendido", PriceFormatter.Format(vm.TotalMonto)),
                    new("Productos distintos", vm.PorProducto.Count.ToString(CultureInfo.InvariantCulture))
                },
                Tablas = { porProducto, porCategoria }
            };
            return await Exportar(formato, $"reporte-ventas_{d:yyyy-MM-dd}_{h:yyyy-MM-dd}", doc);
        }

        // =================================================================
        // INVENTARIO — el archivo solo lleva productos y cantidades
        // =================================================================
        public async Task<IActionResult> ReportInventory([FromServices] ReportService reportService)
        {
            ViewData["Title"] = "Inventario";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetInventoryReportAsync();
            return View(vm);
        }

        public async Task<IActionResult> ReportInventoryExport(string? formato)
        {
            var productos = await context.Products.AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new { p.Name, p.Stock })
                .ToListAsync();

            var tabla = new ReporteTabla
            {
                Titulo = "Productos en inventario",
                Columnas = new[] { "N°", "Producto", "Cantidad" },
                UltimaFilaEsTotal = true
            };
            tabla.Filas.AddRange(productos.Select((p, i) => new object?[] { i + 1, p.Name, p.Stock }));
            tabla.Filas.Add(new object?[] { "", "Total de unidades", productos.Sum(p => p.Stock) });

            var doc = new ReporteDocumento
            {
                Titulo = "Reporte de inventario",
                Descripcion = "Productos registrados y la cantidad disponible de cada uno.",
                Periodo = "al " + Hoy(),
                Resumen =
                {
                    new("Productos", productos.Count.ToString("N0", CultureInfo.InvariantCulture)),
                    new("Unidades en stock", productos.Sum(p => p.Stock).ToString("N0", CultureInfo.InvariantCulture))
                },
                Tablas = { tabla }
            };
            return await Exportar(formato, $"reporte-inventario_{DateTime.UtcNow.AddHours(-4):yyyy-MM-dd}", doc);
        }

        // =================================================================
        // DEMANDA (lo más buscado / guardado)
        // =================================================================
        public async Task<IActionResult> ReportDemand([FromServices] ReportService reportService)
        {
            ViewData["Title"] = "Demanda";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetDemandReportAsync();
            return View(vm);
        }

        public async Task<IActionResult> ReportDemandExport([FromServices] ReportService reportService, string? formato)
        {
            var vm = await reportService.GetDemandReportAsync();

            var favProductos = new ReporteTabla { Titulo = "Productos más guardados en favoritos", Columnas = new[] { "Producto", "Veces guardado" } };
            favProductos.Filas.AddRange(vm.ProductosFavoritos.Select(p => new object?[] { p.Nombre, p.Cantidad }));

            var favRutas = new ReporteTabla { Titulo = "Rutas más guardadas en favoritos", Columnas = new[] { "Ruta", "Veces guardada" } };
            favRutas.Filas.AddRange(vm.RutasFavoritas.Select(r => new object?[] { r.Nombre, r.Cantidad }));

            var carritos = new ReporteTabla { Titulo = "Productos en carritos", Columnas = new[] { "Producto", "Cantidad total", "Carritos distintos" } };
            carritos.Filas.AddRange(vm.ProductosEnCarrito.Select(c => new object?[] { c.ProductName, c.CantidadTotal, c.CarritosDistintos }));

            var doc = new ReporteDocumento
            {
                Titulo = "Reporte de demanda",
                Descripcion = "Lo que más interesa a los clientes: favoritos y productos en carritos.",
                Periodo = "al " + Hoy(),
                Resumen =
                {
                    new("Producto más guardado", vm.ProductosFavoritos.FirstOrDefault()?.Nombre ?? "—"),
                    new("Ruta más guardada", vm.RutasFavoritas.FirstOrDefault()?.Nombre ?? "—"),
                    new("Más veces en carritos", vm.ProductosEnCarrito.FirstOrDefault()?.ProductName ?? "—")
                },
                Tablas = { favProductos, favRutas, carritos }
            };
            return await Exportar(formato, $"reporte-demanda_{DateTime.UtcNow.AddHours(-4):yyyy-MM-dd}", doc);
        }

        // =================================================================
        // USUARIOS
        // =================================================================
        public async Task<IActionResult> ReportUsers([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            ViewData["Title"] = "Usuarios";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetUsersReportAsync(d, h);
            return View(vm);
        }

        public async Task<IActionResult> ReportUsersExport([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta, string? formato)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            var vm = await reportService.GetUsersReportAsync(d, h);

            var porMes = new ReporteTabla { Titulo = "Registros por mes", Columnas = new[] { "Mes", "Usuarios registrados" }, UltimaFilaEsTotal = true };
            porMes.Filas.AddRange(vm.PorMes.Select(m => new object?[] { MesNombre(m.Year, m.Month), m.Cantidad }));
            porMes.Filas.Add(new object?[] { "Total", vm.PorMes.Sum(m => m.Cantidad) });

            var doc = new ReporteDocumento
            {
                Titulo = "Reporte de usuarios",
                Descripcion = "Clientes que crearon su cuenta en el periodo.",
                Periodo = Periodo(d, h),
                Resumen = { new("Total registrados", vm.TotalRegistrados.ToString("N0", CultureInfo.InvariantCulture)) },
                Tablas = { porMes }
            };
            return await Exportar(formato, $"reporte-usuarios_{d:yyyy-MM-dd}_{h:yyyy-MM-dd}", doc);
        }

        // =================================================================
        // GUÍAS Y TRANSPORTES
        // =================================================================
        public async Task<IActionResult> ReportGuidesTransport([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            ViewData["Title"] = "Guías y transportes";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetGuidesTransportReportAsync(d, h);
            return View(vm);
        }

        public async Task<IActionResult> ReportGuidesTransportExport([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta, string? formato)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            var vm = await reportService.GetGuidesTransportReportAsync(d, h);

            var porGuia = new ReporteTabla { Titulo = "Rutas por guía", Columnas = new[] { "Guía", "Rutas asignadas" } };
            porGuia.Filas.AddRange(vm.RutasPorGuia.Select(g => new object?[] { g.GuideName, g.CantidadRutas }));

            var porTransporte = new ReporteTabla { Titulo = "Rutas por transporte", Columnas = new[] { "Transporte", "Rutas asignadas" } };
            porTransporte.Filas.AddRange(vm.RutasPorTransporte.Select(t => new object?[] { t.TransportName, t.CantidadRutas }));

            var reservas = new ReporteTabla { Titulo = "Reservas completadas por guía", Columnas = new[] { "Guía", "Reservas", "Personas", "Monto" }, ColumnasMoneda = new() { 3 } };
            reservas.Filas.AddRange(vm.ReservasPorGuia.Select(r => new object?[] { r.GuideName, r.Cantidad, r.Personas, r.Monto }));

            var doc = new ReporteDocumento
            {
                Titulo = "Reporte de guías y transportes",
                Descripcion = "Asignación de rutas y reservas completadas (Acabado) por guía.",
                Periodo = Periodo(d, h),
                Resumen =
                {
                    new("Guías con rutas", vm.RutasPorGuia.Count.ToString(CultureInfo.InvariantCulture)),
                    new("Transportes con rutas", vm.RutasPorTransporte.Count.ToString(CultureInfo.InvariantCulture)),
                    new("Monto por guías", PriceFormatter.Format(vm.ReservasPorGuia.Sum(r => r.Monto)))
                },
                Tablas = { porGuia, porTransporte, reservas }
            };
            return await Exportar(formato, $"reporte-guias-transportes_{d:yyyy-MM-dd}_{h:yyyy-MM-dd}", doc);
        }
    }
}
