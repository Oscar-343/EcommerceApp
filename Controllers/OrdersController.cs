using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    // Pedidos: el carrito se convierte en pedido, y el usuario ve/cancela los suyos.
    [Authorize]
    public class OrdersController(ApplicationDbContext context) : Controller
    {
        // Confirma el carrito actual como un pedido nuevo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var items = await context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

            if (items.Count == 0)
            {
                TempData["Success"] = "Tu carrito está vacío.";
                return RedirectToAction("Index", "Cart");
            }

            // Revalidar stock en servidor: nunca confiar en lo que se veía en pantalla.
            foreach (var item in items)
            {
                if (item.Product == null)
                {
                    TempData["Success"] = "Un producto de tu carrito ya no existe. Quítalo para continuar.";
                    return RedirectToAction("Index", "Cart");
                }
                if (item.Product.Stock <= 0 || item.Quantity > item.Product.Stock)
                {
                    TempData["Success"] = $"\"{item.Product.Name}\" no tiene stock suficiente. Ajusta la cantidad para continuar.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            await using var transaction = await context.Database.BeginTransactionAsync();

            foreach (var item in items)
            {
                // Descuenta solo si todavía hay stock suficiente. Es una sola operación en la BD,
                // así dos compras simultáneas no pueden vender la misma unidad.
                var filas = await context.Products
                    .Where(p => p.Id == item.ProductId && p.Stock >= item.Quantity)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity));

                if (filas == 0)
                {
                    await transaction.RollbackAsync();
                    TempData["Success"] = $"\"{item.Product!.Name}\" ya no tiene stock suficiente. Ajusta la cantidad para continuar.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            var order = new Order
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = "Pendiente"
            };

            foreach (var item in items)
            {
                var product = item.Product!;
                var precioVigente = product.PromotionalPrice ?? product.Price;

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductCategory = product.Category,
                    UnitPrice = precioVigente,
                    Quantity = item.Quantity
                });
            }

            order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            context.Orders.Add(order);
            context.CartItems.RemoveRange(items);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = "Pedido confirmado.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // "Mis pedidos": lista los del usuario actual, más recientes primero.
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var orders = await context.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewData["Title"] = "Mis pedidos";
            return View(orders);
        }

        // Detalle de un pedido propio.
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var order = await context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();

            ViewData["Title"] = $"Pedido #{order.Id}";
            return View(order);
        }

        // Cancela un pedido propio mientras siga Pendiente; devuelve el stock.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var order = await context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();

            if (order.Status != "Pendiente")
            {
                TempData["Success"] = "Solo se pueden cancelar pedidos pendientes.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await using var transaction = await context.Database.BeginTransactionAsync();

            foreach (var item in order.Items.Where(i => i.ProductId != null))
            {
                // Devuelve el stock directamente en la BD (suma sobre el valor actual, no sobre uno leído antes).
                await context.Products
                    .Where(p => p.Id == item.ProductId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity));
            }

            order.Status = "Cancelado";
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = "Pedido cancelado y stock devuelto.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
