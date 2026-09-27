using EcommerceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    // Sección Productos del panel de administración.
    public partial class AdminController
    {
        // ---------------------------------------------------------------
        // PRODUCTOS
        // ---------------------------------------------------------------

        // stockBajo=true: solo los productos en su stock mínimo o por debajo (enlace de la tarjeta del dashboard).
        public async Task<IActionResult> Products(string? q, bool stockBajo = false)
        {
            ViewData["Title"] = "Productos";
            ViewData["Subtitle"] = "Catálogo de la tienda";

            var query = context.Products.AsNoTracking().Include(p => p.Marca).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => p.Name.ToLower().Contains(q.ToLower()) || (p.Marca != null && p.Marca.Nombre.ToLower().Contains(q.ToLower())));
            }

            if (stockBajo)
            {
                query = query.Where(p => p.Stock <= p.StockMinimo);
            }

            ViewBag.Query = q;
            ViewBag.StockBajo = stockBajo;
            var products = await query.OrderBy(p => p.Name).ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> ProductCreate()
        {
            ViewData["Title"] = "Nuevo producto";
            ViewData["Subtitle"] = "Productos";
            await CargarMarcasAsync(null);
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> ProductCreate(
            Product product,
            IFormFile? imageFile,
            IFormFile? secondaryImageFile,
            List<IFormFile>? galleryFiles)
        {
            ViewData["Title"] = "Nuevo producto";
            ViewData["Subtitle"] = "Productos";
            await CargarMarcasAsync(product.MarcaId);
            await ValidarMarcaAsync(product.MarcaId, marcaInactivaPermitida: null);

            try
            {
                // Validar primero: si el formulario tiene errores, no se sube nada a Supabase.
                if (!ModelState.IsValid)
                    return View(product);

                // Solo cuenta MarcaId: si alguien envía campos "Marca.Nombre", EF crearía una marca nueva.
                product.Marca = null;

                if (imageFile != null && imageFile.Length > 0)
                    product.ImageUrl = await UploadAdminImageAsync(imageFile, "products");

                if (secondaryImageFile != null && secondaryImageFile.Length > 0)
                    product.SecondaryImageUrl = await UploadAdminImageAsync(secondaryImageFile, "products");

                var galleryUrls = await UploadGalleryAsync(galleryFiles, "products/gallery");
                if (!string.IsNullOrWhiteSpace(galleryUrls))
                    product.GalleryImages = AppendPipeSeparated(product.GalleryImages, galleryUrls);

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
            await CargarMarcasAsync(product.MarcaId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> ProductEdit(
            int id,
            Product product,
            IFormFile? imageFile,
            IFormFile? secondaryImageFile,
            List<IFormFile>? galleryFiles)
        {
            ViewData["Title"] = "Editar producto";
            ViewData["Subtitle"] = "Productos";

            if (id != product.Id) return NotFound();

            // Si el producto ya tenía una marca que luego se desactivó, se puede conservar.
            var marcaOriginal = await context.Products.Where(p => p.Id == id).Select(p => p.MarcaId).FirstOrDefaultAsync();
            await CargarMarcasAsync(product.MarcaId ?? marcaOriginal);
            await ValidarMarcaAsync(product.MarcaId, marcaInactivaPermitida: marcaOriginal);

            try
            {
                // Validar primero: si el formulario tiene errores, no se sube nada a Supabase.
                if (!ModelState.IsValid)
                    return View(product);

                // Solo cuenta MarcaId: si alguien envía campos "Marca.Nombre", EF crearía una marca nueva.
                product.Marca = null;

                if (imageFile != null && imageFile.Length > 0)
                    product.ImageUrl = await UploadAdminImageAsync(imageFile, "products");

                if (secondaryImageFile != null && secondaryImageFile.Length > 0)
                    product.SecondaryImageUrl = await UploadAdminImageAsync(secondaryImageFile, "products");

                var galleryUrls = await UploadGalleryAsync(galleryFiles, "products/gallery");
                if (!string.IsNullOrWhiteSpace(galleryUrls))
                    product.GalleryImages = AppendPipeSeparated(product.GalleryImages, galleryUrls);

                product.UpdatedAt = DateTime.UtcNow;
                context.Products.Update(product);
                // Update() marca toda la entidad como modificada; CreatedAt no debe pisarse con el valor del formulario.
                context.Entry(product).Property(p => p.CreatedAt).IsModified = false;
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
                // Borra también los favoritos que apuntan a este producto (no tienen FK real, quedarían huérfanos).
                var favoritos = context.FavoriteItems.Where(f => f.Type == "Product" && f.ItemId == id);
                context.FavoriteItems.RemoveRange(favoritos);

                context.Products.Remove(product);
                await context.SaveChangesAsync();
                TempData["Success"] = $"Producto \"{product.Name}\" eliminado.";
            }
            return RedirectToAction(nameof(Products));
        }

        // Marcas para el select del formulario: las activas, más la que ya tiene el producto aunque esté inactiva.
        private async Task CargarMarcasAsync(int? marcaActual)
        {
            ViewBag.Marcas = await context.Marcas.AsNoTracking()
                .Where(m => m.Activo || m.Id == marcaActual)
                .OrderBy(m => m.Nombre)
                .ToListAsync();
        }

        // La marca elegida debe existir y estar activa (salvo la que el producto ya tenía).
        private async Task ValidarMarcaAsync(int? marcaId, int? marcaInactivaPermitida)
        {
            if (!marcaId.HasValue) return;

            var valida = await context.Marcas.AnyAsync(m => m.Id == marcaId && (m.Activo || m.Id == marcaInactivaPermitida));
            if (!valida)
                ModelState.AddModelError(nameof(Product.MarcaId), "Elige una marca activa de la lista.");
        }
    }
}
