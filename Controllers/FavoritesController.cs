using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    // Favoritos: productos y rutas, accesible para usuarios autenticados.
    [Authorize]
    public class FavoritesController(ApplicationDbContext context) : Controller
    {
        // Lista de favoritos del usuario (productos + rutas).
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var favorites = await context.FavoriteItems
                .Where(f => f.UserId == userId)
                .ToListAsync();

            var productIds = favorites.Where(f => f.Type == "Product").Select(f => f.ItemId).ToList();
            var serviceIds = favorites.Where(f => f.Type == "Service").Select(f => f.ItemId).ToList();

            ViewBag.Products = await context.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();
            ViewBag.Services = await context.Services.AsNoTracking()
                .Where(s => serviceIds.Contains(s.Id))
                .ToListAsync();

            ViewData["Title"] = "Mis favoritos";
            return View(favorites);
        }

        // Agrega o quita un favorito según el tipo (Product / Service).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(string type, int itemId)
        {
            var esAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (type != "Product" && type != "Service")
            {
                return esAjax ? BadRequest(new { success = false, message = "Tipo inválido." }) : BadRequest();
            }

            var existeItem = type == "Product"
                ? await context.Products.AnyAsync(p => p.Id == itemId)
                : await context.Services.AnyAsync(s => s.Id == itemId);
            if (!existeItem)
            {
                return esAjax ? NotFound(new { success = false, message = "No existe." }) : NotFound();
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var existing = await context.FavoriteItems
                .FirstOrDefaultAsync(f => f.UserId == userId && f.Type == type && f.ItemId == itemId);

            bool esFavoritoAhora;
            if (existing != null)
            {
                context.FavoriteItems.Remove(existing);
                await context.SaveChangesAsync();
                TempData["Success"] = "Eliminado de favoritos.";
                esFavoritoAhora = false;
            }
            else
            {
                context.FavoriteItems.Add(new FavoriteItem
                {
                    UserId = userId,
                    Type = type,
                    ItemId = itemId
                });
                await context.SaveChangesAsync();
                TempData["Success"] = "Agregado a favoritos.";
                esFavoritoAhora = true;
            }

            if (esAjax) return Json(new { success = true, isFavorite = esFavoritoAhora });
            return RedirectToAction(nameof(Index));
        }
    }
}