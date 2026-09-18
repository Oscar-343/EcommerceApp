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
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var existing = await context.FavoriteItems
                .FirstOrDefaultAsync(f => f.UserId == userId && f.Type == type && f.ItemId == itemId);

            if (existing != null)
            {
                context.FavoriteItems.Remove(existing);
                await context.SaveChangesAsync();
                TempData["Success"] = "Eliminado de favoritos.";
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
            }
            return RedirectToAction(nameof(Index));
        }
    }
}