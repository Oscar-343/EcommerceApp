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

        public async Task<IActionResult> Products(string? q)
        {
            ViewData["Title"] = "Productos";
            ViewData["Subtitle"] = "Catálogo de la tienda";

            var query = context.Products.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => p.Name.ToLower().Contains(q.ToLower()) || (p.Brand != null && p.Brand.ToLower().Contains(q.ToLower())));
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
            IFormFile? secondaryImageFile,
            List<IFormFile>? galleryFiles)
        {
            ViewData["Title"] = "Nuevo producto";
            ViewData["Subtitle"] = "Productos";

            try
            {
                if (imageFile != null && imageFile.Length > 0)
                    product.ImageUrl = await UploadAdminImageAsync(imageFile, "products");

                if (secondaryImageFile != null && secondaryImageFile.Length > 0)
                    product.SecondaryImageUrl = await UploadAdminImageAsync(secondaryImageFile, "products");

                var galleryUrls = await UploadGalleryAsync(galleryFiles, "products/gallery");
                if (!string.IsNullOrWhiteSpace(galleryUrls))
                    product.GalleryImages = AppendPipeSeparated(product.GalleryImages, galleryUrls);

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
            IFormFile? secondaryImageFile,
            List<IFormFile>? galleryFiles)
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

                var galleryUrls = await UploadGalleryAsync(galleryFiles, "products/gallery");
                if (!string.IsNullOrWhiteSpace(galleryUrls))
                    product.GalleryImages = AppendPipeSeparated(product.GalleryImages, galleryUrls);

                if (!ModelState.IsValid)
                    return View(product);

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
    }
}
