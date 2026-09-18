using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    // Carrito: solo productos, accesible para usuarios autenticados.
    [Authorize]
    public class CartController(ApplicationDbContext context) : Controller
    {
        // Muestra el carrito del usuario actual con totales y cantidades.
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var items = await context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

            ViewBag.Total = items.Sum(i => i.Quantity * i.UnitPrice);
            ViewData["Title"] = "Tu carrito";
            return View(items);
        }

        // Agrega un producto al carrito (o aumenta cantidad si ya existe).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var product = await context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var existing = await context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.PromotionalPrice ?? product.Price
                });
            }
            await context.SaveChangesAsync();
            TempData["Success"] = "Producto agregado al carrito.";
            return RedirectToAction("Index", "Cart");
        }

        // Elimina un producto del carrito.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int productId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var item = await context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
            if (item != null)
            {
                context.CartItems.Remove(item);
                await context.SaveChangesAsync();
                TempData["Success"] = "Producto eliminado del carrito.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}