using EcommerceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    // Sección Rutas/Servicios del panel de administración.
    public partial class AdminController
    {
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
                query = query.Where(s => s.Name.ToLower().Contains(q.ToLower()) || (s.Region != null && s.Region.ToLower().Contains(q.ToLower())));
            }

            ViewBag.Query = q;
            var services = await query.OrderBy(s => s.Name).ToListAsync();
            return View(services);
        }

        private async Task LoadServiceLookupsAsync()
        {
            ViewBag.Guides = await context.Guides.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
            ViewBag.Transports = await context.Transports.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync();
            ViewBag.Products = await context.Products.AsNoTracking().OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync();
        }

        public async Task<IActionResult> ServiceCreate()
        {
            ViewData["Title"] = "Nueva ruta";
            ViewData["Subtitle"] = "Rutas / Servicios";
            await LoadServiceLookupsAsync();
            ViewBag.SelectedProductIds = new List<int>();
            return View(new Service());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(30 * 1024 * 1024)]
        public async Task<IActionResult> ServiceCreate(
            Service service,
            IFormFile? imageFile,
            IFormFile? secondaryImageFile,
            List<IFormFile>? galleryFiles,
            List<int>? productIds)
        {
            ViewData["Title"] = "Nueva ruta";
            ViewData["Subtitle"] = "Rutas / Servicios";

            try
            {
                // Validar primero: si el formulario tiene errores, no se sube nada a Supabase.
                if (!ModelState.IsValid)
                {
                    await LoadServiceLookupsAsync();
                    ViewBag.SelectedProductIds = productIds ?? new List<int>();
                    return View(service);
                }

                if (imageFile != null && imageFile.Length > 0)
                    service.ImageUrl = await UploadAdminImageAsync(imageFile, "services");

                if (secondaryImageFile != null && secondaryImageFile.Length > 0)
                    service.SecondaryImageUrl = await UploadAdminImageAsync(secondaryImageFile, "services");

                var galleryUrls = await UploadGalleryAsync(galleryFiles, "services/gallery");
                if (!string.IsNullOrWhiteSpace(galleryUrls))
                    service.GalleryImages = AppendPipeSeparated(service.GalleryImages, galleryUrls);

                context.Services.Add(service);
                await context.SaveChangesAsync();

                if (productIds != null && productIds.Count > 0)
                {
                    context.ServiceProducts.AddRange(
                        productIds.Distinct().Select(pid => new ServiceProduct { ServiceId = service.Id, ProductId = pid }));
                    await context.SaveChangesAsync();
                }

                TempData["Success"] = $"Ruta \"{service.Name}\" creada correctamente.";
                return RedirectToAction(nameof(Services));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadServiceLookupsAsync();
                ViewBag.SelectedProductIds = productIds ?? new List<int>();
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
            ViewBag.SelectedProductIds = await context.ServiceProducts.AsNoTracking()
                .Where(sp => sp.ServiceId == id)
                .Select(sp => sp.ProductId)
                .ToListAsync();
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
            List<IFormFile>? galleryFiles,
            List<int>? productIds)
        {
            ViewData["Title"] = "Editar ruta";
            ViewData["Subtitle"] = "Rutas / Servicios";

            if (id != service.Id) return NotFound();

            try
            {
                // Validar primero: si el formulario tiene errores, no se sube nada a Supabase.
                if (!ModelState.IsValid)
                {
                    await LoadServiceLookupsAsync();
                    ViewBag.SelectedProductIds = productIds ?? new List<int>();
                    return View(service);
                }

                if (imageFile != null && imageFile.Length > 0)
                    service.ImageUrl = await UploadAdminImageAsync(imageFile, "services");

                if (secondaryImageFile != null && secondaryImageFile.Length > 0)
                    service.SecondaryImageUrl = await UploadAdminImageAsync(secondaryImageFile, "services");

                var galleryUrls = await UploadGalleryAsync(galleryFiles, "services/gallery");
                if (!string.IsNullOrWhiteSpace(galleryUrls))
                    service.GalleryImages = AppendPipeSeparated(service.GalleryImages, galleryUrls);

                service.UpdatedAt = DateTime.UtcNow;
                context.Services.Update(service);
                // Update() marca toda la entidad como modificada; CreatedAt no debe pisarse con el valor del formulario.
                context.Entry(service).Property(s => s.CreatedAt).IsModified = false;

                // Reemplaza el equipamiento asignado: se borra lo anterior y se agrega lo elegido ahora.
                var existingLinks = context.ServiceProducts.Where(sp => sp.ServiceId == id);
                context.ServiceProducts.RemoveRange(existingLinks);
                if (productIds != null && productIds.Count > 0)
                {
                    context.ServiceProducts.AddRange(
                        productIds.Distinct().Select(pid => new ServiceProduct { ServiceId = id, ProductId = pid }));
                }

                await context.SaveChangesAsync();

                TempData["Success"] = $"Ruta \"{service.Name}\" actualizada.";
                return RedirectToAction(nameof(Services));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadServiceLookupsAsync();
                ViewBag.SelectedProductIds = productIds ?? new List<int>();
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
                // Reservation.Service es Restrict: si hay reservas asociadas, no se puede borrar la ruta.
                var tieneReservas = await context.Reservations.AnyAsync(r => r.ServiceId == id);
                if (tieneReservas)
                {
                    TempData["Success"] = $"No se puede eliminar \"{service.Name}\": tiene reservas asociadas.";
                    return RedirectToAction(nameof(Services));
                }

                // Borra también los favoritos que apuntan a esta ruta (no tienen FK real, quedarían huérfanos).
                var favoritos = context.FavoriteItems.Where(f => f.Type == "Service" && f.ItemId == id);
                context.FavoriteItems.RemoveRange(favoritos);

                context.Services.Remove(service);
                await context.SaveChangesAsync();
                TempData["Success"] = $"Ruta \"{service.Name}\" eliminada.";
            }
            return RedirectToAction(nameof(Services));
        }
    }
}
