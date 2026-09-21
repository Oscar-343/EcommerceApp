using EcommerceApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApp.Controllers
{
    // Sección Reportes del panel de administración.
    public partial class AdminController
    {
        // Índice con una tarjeta por reporte.
        public IActionResult Reports()
        {
            ViewData["Title"] = "Reportes";
            ViewData["Subtitle"] = "Ingresos, reservas, ventas e inventario";
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

        public async Task<IActionResult> ReportReservations(
            [FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta, string? estado, int? serviceId)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            ViewData["Title"] = "Reservas";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetReservationsReportAsync(d, h, estado, serviceId);
            return View(vm);
        }

        public async Task<IActionResult> ReportProductSales([FromServices] ReportService reportService, DateOnly? desde, DateOnly? hasta)
        {
            var (d, h) = ReportService.DefaultRange(desde, hasta);
            ViewData["Title"] = "Ventas de productos";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetProductSalesReportAsync(d, h);
            return View(vm);
        }

        public async Task<IActionResult> ReportInventory([FromServices] ReportService reportService)
        {
            ViewData["Title"] = "Inventario";
            ViewData["Subtitle"] = "Reportes";
            var vm = await reportService.GetInventoryReportAsync();
            return View(vm);
        }
    }
}
