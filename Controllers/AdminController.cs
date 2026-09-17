using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    // Panel de administración: solo usuarios con rol Admin.
    // CRUD de productos, rutas/servicios, guías, transporte y gestión de reservas.
    [Authorize(Roles = "Admin")]
    public class AdminController(ApplicationDbContext context, IImageStorageService imageStorage) : Controller
    {
        private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
        private static readonly string[] AllowedVideoTypes = { "video/mp4", "video/webm" };
        private const long MaxImageBytes = 5 * 1024 * 1024;     // 5 MB
        private const long MaxVideoBytes = 100 * 1024 * 1024;   // 100 MB

        // ---------------------------------------------------------------
        // SUBIDA DE IMÁGENES / VIDEO (Supabase Storage)
        // ---------------------------------------------------------------
        // Se llama por AJAX desde los formularios de Productos, Rutas y Branding.
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

        // ---------------------------------------------------------------
        // BRANDING (logo, video de fondo, imagen de login...)
        // ---------------------------------------------------------------
        // No se guarda en base de datos: solo sube el archivo a Supabase y te
        // muestra la URL pública para que la pegues donde corresponda en el código.
        public IActionResult Branding()
        {
            ViewData["Title"] = "Branding";
            ViewData["Subtitle"] = "Logo, video de fondo y otras imágenes de marca";
            return View();
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
        // PRODUCTOS
        // ---------------------------------------------------------------

        public async Task<IActionResult> Products(string? q)
        {
            ViewData["Title"] = "Productos";
            ViewData["Subtitle"] = "Catálogo de la tienda";

            var query = context.Products.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => p.Name.Contains(q) || (p.Brand != null && p.Brand.Contains(q)));
            }

            ViewBag.Query = q;
            var products = await query.OrderBy(p => p.Name).ToListAsync();
            return View(products);
        }

        public IActionResult ProductCreate()
        {
            ViewData["Title"] = "Nuevo producto";
            ViewData["Subtitle"] = "Productos";
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> ProductCreate(
            Product product,
            IFormFile? imageFile,
            IFormFile? secondaryImageFile)
        {
            ViewData["Title"] = "Nuevo producto";
            ViewData["Subtitle"] = "Productos";

            try
            {
                if (imageFile != null && imageFile.Length > 0)
                    product.ImageUrl = await UploadAdminImageAsync(imageFile, "products");

                if (secondaryImageFile != null && secondaryImageFile.Length > 0)
                    product.SecondaryImageUrl = await UploadAdminImageAsync(secondaryImageFile, "products");

                if (!ModelState.IsValid)
                    return View(product);

                context.Products.Add(product);
                await context.SaveChangesAsync();

                TempData["Success"] = $"Producto \"{product.Name}\" creado correctamente.";
                return RedirectToAction(nameof(Products));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(product);
            }
        }

        public async Task<IActionResult> ProductEdit(int id)
        {
            ViewData["Title"] = "Editar producto";
            ViewData["Subtitle"] = "Productos";
            var product = await context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> ProductEdit(
            int id,
            Product product,
            IFormFile? imageFile,
            IFormFile? secondaryImageFile)
        {
            ViewData["Title"] = "Editar producto";
            ViewData["Subtitle"] = "Productos";

            if (id != product.Id) return NotFound();

            try
            {
                if (imageFile != null && imageFile.Length > 0)
                    product.ImageUrl = await UploadAdminImageAsync(imageFile, "products");

                if (secondaryImageFile != null && secondaryImageFile.Length > 0)
                    product.SecondaryImageUrl = await UploadAdminImageAsync(secondaryImageFile, "products");

                if (!ModelState.IsValid)
                    return View(product);

                product.UpdatedAt = DateTime.UtcNow;
                context.Products.Update(product);
                await context.SaveChangesAsync();

                TempData["Success"] = $"Producto \"{product.Name}\" actualizado.";
                return RedirectToAction(nameof(Products));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(product);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProductDelete(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product != null)
            {
                context.Products.Remove(product);
                await context.SaveChangesAsync();
                TempData["Success"] = $"Producto \"{product.Name}\" eliminado.";
            }
            return RedirectToAction(nameof(Products));
        }

        // ---------------------------------------------------------------
        // SERVICIOS / RUTAS
        // ---------------------------------------------------------------

        public async Task<IActionResult> Services(string? q)
        {
            ViewData["Title"] = "Rutas / Servicios";
            ViewData["Subtitle"] = "Catálogo de senderismo y aventura";

            var query = context.Services.AsNoTracking().Include(s => s.Guide).Include(s => s.Transport).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(s => s.Name.Contains(q) || (s.Region != null && s.Region.Contains(q)));
            }

            ViewBag.Query = q;
            var services = await query.OrderBy(s => s.Name).ToListAsync();
            return View(services);
        }

        private async Task LoadServiceLookupsAsync()
        {
            ViewBag.Guides = await context.Guides.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
            ViewBag.Transports = await context.Transports.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync();
        }

        public async Task<IActionResult> ServiceCreate()
        {
            ViewData["Title"] = "Nueva ruta";
            ViewData["Subtitle"] = "Rutas / Servicios";
            await LoadServiceLookupsAsync();
            return View(new Service());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(30 * 1024 * 1024)]
        public async Task<IActionResult> ServiceCreate(
            Service service,
            IFormFile? imageFile,
            IFormFile? secondaryImageFile,
            List<IFormFile>? galleryFiles)
        {
            ViewData["Title"] = "Nueva ruta";
            ViewData["Subtitle"] = "Rutas / Servicios";

            try
            {
                if (imageFile != null && imageFile.Length > 0)
                    service.ImageUrl = await UploadAdminImageAsync(imageFile, "services");

                if (secondaryImageFile != null && secondaryImageFile.Length > 0)
                    service.SecondaryImageUrl = await UploadAdminImageAsync(secondaryImageFile, "services");

                var galleryUrls = await UploadGalleryAsync(galleryFiles, "services/gallery");
                if (!string.IsNullOrWhiteSpace(galleryUrls))
                    service.GalleryImages = AppendPipeSeparated(service.GalleryImages, galleryUrls);

                if (!ModelState.IsValid)
                {
                    await LoadServiceLookupsAsync();
                    return View(service);
                }

                context.Services.Add(service);
                await context.SaveChangesAsync();

                TempData["Success"] = $"Ruta \"{service.Name}\" creada correctamente.";
                return RedirectToAction(nameof(Services));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadServiceLookupsAsync();
                return View(service);
            }
        }

        public async Task<IActionResult> ServiceEdit(int id)
        {
            ViewData["Title"] = "Editar ruta";
            ViewData["Subtitle"] = "Rutas / Servicios";
            var service = await context.Services.FindAsync(id);
            if (service == null) return NotFound();
            await LoadServiceLookupsAsync();
            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(30 * 1024 * 1024)]
        public async Task<IActionResult> ServiceEdit(
            int id,
            Service service,
            IFormFile? imageFile,
            IFormFile? secondaryImageFile,
            List<IFormFile>? galleryFiles)
        {
            ViewData["Title"] = "Editar ruta";
            ViewData["Subtitle"] = "Rutas / Servicios";

            if (id != service.Id) return NotFound();

            try
            {
                if (imageFile != null && imageFile.Length > 0)
                    service.ImageUrl = await UploadAdminImageAsync(imageFile, "services");

                if (secondaryImageFile != null && secondaryImageFile.Length > 0)
                    service.SecondaryImageUrl = await UploadAdminImageAsync(secondaryImageFile, "services");

                var galleryUrls = await UploadGalleryAsync(galleryFiles, "services/gallery");
                if (!string.IsNullOrWhiteSpace(galleryUrls))
                    service.GalleryImages = AppendPipeSeparated(service.GalleryImages, galleryUrls);

                if (!ModelState.IsValid)
                {
                    await LoadServiceLookupsAsync();
                    return View(service);
                }

                service.UpdatedAt = DateTime.UtcNow;
                context.Services.Update(service);
                await context.SaveChangesAsync();

                TempData["Success"] = $"Ruta \"{service.Name}\" actualizada.";
                return RedirectToAction(nameof(Services));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadServiceLookupsAsync();
                return View(service);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ServiceDelete(int id)
        {
            var service = await context.Services.FindAsync(id);
            if (service != null)
            {
                context.Services.Remove(service);
                await context.SaveChangesAsync();
                TempData["Success"] = $"Ruta \"{service.Name}\" eliminada.";
            }
            return RedirectToAction(nameof(Services));
        }

        // ---------------------------------------------------------------
        // GUÍAS
        // ---------------------------------------------------------------

        public async Task<IActionResult> Guides(string? q)
        {
            ViewData["Title"] = "Guías";
            ViewData["Subtitle"] = "Equipo de guías de montaña";

            var query = context.Guides.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(g => g.Name.Contains(q) || (g.Specialty != null && g.Specialty.Contains(q)));
            }

            ViewBag.Query = q;
            var guides = await query.OrderBy(g => g.Name).ToListAsync();

            // Cantidad de rutas asignadas por guía, para mostrar en la tabla.
            var counts = await context.Services
                .Where(s => s.GuideId != null)
                .GroupBy(s => s.GuideId)
                .Select(g => new { GuideId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GuideId!.Value, x => x.Count);
            ViewBag.RouteCounts = counts;

            return View(guides);
        }

        public IActionResult GuideCreate()
        {
            ViewData["Title"] = "Nuevo guía";
            ViewData["Subtitle"] = "Guías";
            return View(new Guide());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuideCreate(Guide guide)
        {
            ViewData["Title"] = "Nuevo guía";
            ViewData["Subtitle"] = "Guías";
            if (!ModelState.IsValid) return View(guide);

            context.Guides.Add(guide);
            await context.SaveChangesAsync();
            TempData["Success"] = $"Guía \"{guide.Name}\" agregado.";
            return RedirectToAction(nameof(Guides));
        }

        public async Task<IActionResult> GuideEdit(int id)
        {
            ViewData["Title"] = "Editar guía";
            ViewData["Subtitle"] = "Guías";
            var guide = await context.Guides.FindAsync(id);
            if (guide == null) return NotFound();
            return View(guide);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuideEdit(int id, Guide guide)
        {
            ViewData["Title"] = "Editar guía";
            ViewData["Subtitle"] = "Guías";
            if (id != guide.Id) return NotFound();
            if (!ModelState.IsValid) return View(guide);

            guide.UpdatedAt = DateTime.UtcNow;
            context.Guides.Update(guide);
            await context.SaveChangesAsync();
            TempData["Success"] = $"Guía \"{guide.Name}\" actualizado.";
            return RedirectToAction(nameof(Guides));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuideDelete(int id)
        {
            var guide = await context.Guides.FindAsync(id);
            if (guide != null)
            {
                var inUse = await context.Services.AnyAsync(s => s.GuideId == id);
                if (inUse)
                {
                    // No se puede borrar: hay rutas que lo referencian. Se desactiva en su lugar.
                    guide.IsActive = false;
                    guide.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    TempData["Success"] = $"\"{guide.Name}\" tiene rutas asignadas, así que se marcó como inactivo en vez de eliminarse.";
                }
                else
                {
                    context.Guides.Remove(guide);
                    await context.SaveChangesAsync();
                    TempData["Success"] = $"Guía \"{guide.Name}\" eliminado.";
                }
            }
            return RedirectToAction(nameof(Guides));
        }

        // ---------------------------------------------------------------
        // TRANSPORTE
        // ---------------------------------------------------------------

        public async Task<IActionResult> Transports(string? q)
        {
            ViewData["Title"] = "Transporte";
            ViewData["Subtitle"] = "Vehículos disponibles para traslados";

            var query = context.Transports.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(t => t.Name.Contains(q) || (t.Type != null && t.Type.Contains(q)));
            }

            ViewBag.Query = q;
            var transports = await query.OrderBy(t => t.Name).ToListAsync();

            var counts = await context.Services
                .Where(s => s.TransportId != null)
                .GroupBy(s => s.TransportId)
                .Select(g => new { TransportId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TransportId!.Value, x => x.Count);
            ViewBag.RouteCounts = counts;

            return View(transports);
        }

        public IActionResult TransportCreate()
        {
            ViewData["Title"] = "Nuevo transporte";
            ViewData["Subtitle"] = "Transporte";
            return View(new Transport());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransportCreate(Transport transport)
        {
            ViewData["Title"] = "Nuevo transporte";
            ViewData["Subtitle"] = "Transporte";
            if (!ModelState.IsValid) return View(transport);

            context.Transports.Add(transport);
            await context.SaveChangesAsync();
            TempData["Success"] = $"Transporte \"{transport.Name}\" agregado.";
            return RedirectToAction(nameof(Transports));
        }

        public async Task<IActionResult> TransportEdit(int id)
        {
            ViewData["Title"] = "Editar transporte";
            ViewData["Subtitle"] = "Transporte";
            var transport = await context.Transports.FindAsync(id);
            if (transport == null) return NotFound();
            return View(transport);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransportEdit(int id, Transport transport)
        {
            ViewData["Title"] = "Editar transporte";
            ViewData["Subtitle"] = "Transporte";
            if (id != transport.Id) return NotFound();
            if (!ModelState.IsValid) return View(transport);

            transport.UpdatedAt = DateTime.UtcNow;
            context.Transports.Update(transport);
            await context.SaveChangesAsync();
            TempData["Success"] = $"Transporte \"{transport.Name}\" actualizado.";
            return RedirectToAction(nameof(Transports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransportDelete(int id)
        {
            var transport = await context.Transports.FindAsync(id);
            if (transport != null)
            {
                var inUse = await context.Services.AnyAsync(s => s.TransportId == id);
                if (inUse)
                {
                    transport.IsActive = false;
                    transport.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    TempData["Success"] = $"\"{transport.Name}\" está asignado a rutas, así que se marcó como inactivo en vez de eliminarse.";
                }
                else
                {
                    context.Transports.Remove(transport);
                    await context.SaveChangesAsync();
                    TempData["Success"] = $"Transporte \"{transport.Name}\" eliminado.";
                }
            }
            return RedirectToAction(nameof(Transports));
        }

        // ---------------------------------------------------------------
        // RESERVAS
        // ---------------------------------------------------------------

        public async Task<IActionResult> Reservations(string? status)
        {
            ViewData["Title"] = "Reservas";
            ViewData["Subtitle"] = "Reservas de rutas hechas por los usuarios";

            var query = context.Reservations.AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Service)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            ViewBag.StatusFilter = status;
            var reservations = await query.OrderByDescending(r => r.BookingDate).ToListAsync();
            return View(reservations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservationUpdateStatus(string userId, int serviceId, string status)
        {
            var reservation = await context.Reservations.FindAsync(userId, serviceId);
            if (reservation != null)
            {
                reservation.Status = status;
                await context.SaveChangesAsync();
                TempData["Success"] = "Estado de la reserva actualizado.";
            }
            return RedirectToAction(nameof(Reservations));
        }

        // ---------------------------------------------------------------
        // HELPERS DE SUBIDA DIRECTA A SUPABASE
        // ---------------------------------------------------------------

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