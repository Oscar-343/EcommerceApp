using EcommerceApp.Data;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    // Panel de administración: solo usuarios con rol Admin.
    // CRUD de productos, rutas/servicios, guías, transporte y gestión de reservas.
    // Dividido en clases parciales por entidad (AdminController.Products.cs, .Services.cs, .Guides.cs,
    // .Transports.cs, .Reservations.cs); este archivo tiene el constructor, el dashboard y los helpers compartidos.
    [Authorize(Roles = "Admin")]
    public partial class AdminController(ApplicationDbContext context, IImageStorageService imageStorage) : Controller
    {
        private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
        private static readonly string[] AllowedVideoTypes = { "video/mp4", "video/webm" };
        private const long MaxImageBytes = 5 * 1024 * 1024;     // 5 MB
        private const long MaxVideoBytes = 100 * 1024 * 1024;   // 100 MB

        // ---------------------------------------------------------------
        // SUBIDA DE IMÁGENES / VIDEO (Supabase Storage)
        // ---------------------------------------------------------------
        // Se llama por AJAX desde los formularios de Productos y Rutas.
        // "folder" agrupa los archivos dentro del bucket: products, services, branding...
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(120 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(List<IFormFile> files, string folder = "misc")
        {
            if (files == null || files.Count == 0)
            {
                return Json(new { success = false, error = "No se recibió ningún archivo." });
            }

            var urls = new List<string>();
            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var isVideo = AllowedVideoTypes.Contains(file.ContentType);
                var isImage = AllowedImageTypes.Contains(file.ContentType);

                if (!isVideo && !isImage)
                {
                    return Json(new { success = false, error = $"\"{file.FileName}\" no es un tipo permitido (imagen JPG/PNG/WEBP/GIF o video MP4/WEBM)." });
                }

                var limit = isVideo ? MaxVideoBytes : MaxImageBytes;
                if (file.Length > limit)
                {
                    var limitLabel = isVideo ? "100 MB" : "5 MB";
                    return Json(new { success = false, error = $"\"{file.FileName}\" supera el tamaño máximo de {limitLabel}." });
                }

                try
                {
                    await using var stream = file.OpenReadStream();
                    var url = await imageStorage.UploadAsync(stream, file.FileName, file.ContentType, folder);
                    urls.Add(url);
                }
                catch (InvalidOperationException ex)
                {
                    return Json(new { success = false, error = ex.Message });
                }
            }

            if (urls.Count == 0)
            {
                return Json(new { success = false, error = "No se pudo subir ningún archivo." });
            }

            return Json(new { success = true, urls, url = urls[0] });
        }

        // Disponible en todas las vistas del panel (badge de reservas pendientes en el sidebar).
        public override async Task OnActionExecutionAsync(
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext ctx,
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
        {
            try
            {
                ViewBag.PendingReservations = await context.Reservations.CountAsync(r => r.Status == "Pendiente");
            }
            catch
            {
                ViewBag.PendingReservations = 0;
            }

            try
            {
                ViewBag.PendingOrders = await context.Orders.CountAsync(o => o.Status == "Pendiente");
            }
            catch
            {
                ViewBag.PendingOrders = 0;
            }

            await next();
        }

        // ---------------------------------------------------------------
        // DASHBOARD
        // ---------------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Resumen";
            ViewData["Subtitle"] = "Estado general del negocio";

            ViewBag.ProductCount = await context.Products.CountAsync();
            ViewBag.ServiceCount = await context.Services.CountAsync();
            ViewBag.GuideCount = await context.Guides.CountAsync(g => g.IsActive);
            ViewBag.TransportCount = await context.Transports.CountAsync(t => t.IsActive);
            ViewBag.ReservationCount = await context.Reservations.CountAsync();
            ViewBag.LowStockCount = await context.Products.CountAsync(p => p.Stock <= 5);
            ViewBag.OrderCount = await context.Orders.CountAsync();

            ViewBag.RecentReservations = await context.Reservations
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Service)
                .OrderByDescending(r => r.BookingDate)
                .Take(6)
                .ToListAsync();

            return View();
        }

        // ---------------------------------------------------------------
        // HELPERS DE SUBIDA DIRECTA A SUPABASE
        // ---------------------------------------------------------------
        // Compartidos por AdminController.Products.cs y AdminController.Services.cs.

        private async Task<string?> UploadAdminImageAsync(IFormFile? file, string folder)
        {
            if (file == null || file.Length == 0)
                return null;

            if (!AllowedImageTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"El archivo \"{file.FileName}\" no es una imagen válida. Usa JPG, PNG, WEBP o GIF.");

            if (file.Length > MaxImageBytes)
                throw new InvalidOperationException(
                    $"La imagen \"{file.FileName}\" supera el máximo permitido de 5 MB.");

            await using var stream = file.OpenReadStream();

            return await imageStorage.UploadAsync(
                stream,
                file.FileName,
                file.ContentType,
                folder);
        }

        private async Task<string?> UploadGalleryAsync(List<IFormFile>? files, string folder)
        {
            if (files == null || files.Count == 0)
                return null;

            var urls = new List<string>();

            foreach (var file in files)
            {
                var url = await UploadAdminImageAsync(file, folder);
                if (!string.IsNullOrWhiteSpace(url))
                    urls.Add(url);
            }

            return urls.Count > 0 ? string.Join("|", urls) : null;
        }

        private static string? AppendPipeSeparated(string? existing, string? added)
        {
            var oldValue = existing?.Trim('|', ' ', '\t', '\r', '\n');
            var newValue = added?.Trim('|', ' ', '\t', '\r', '\n');

            if (string.IsNullOrWhiteSpace(oldValue)) return newValue;
            if (string.IsNullOrWhiteSpace(newValue)) return oldValue;

            return $"{oldValue}|{newValue}";
        }
    }
}
