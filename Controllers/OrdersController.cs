using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;

namespace EcommerceApp.Controllers
{
    // Pedidos: el carrito se convierte en pedido, y el usuario ve/cancela los suyos.
    [Authorize]
    public class OrdersController(ApplicationDbContext context, PedidoService pedidoService) : Controller
    {
        // Confirma el carrito actual como un pedido nuevo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

            // Revalida stock, descuenta, crea el pedido y vacía el carrito (ver PedidoService).
            var resultado = await pedidoService.CrearDesdeCarritoAsync(userId);
            if (resultado.Pedido == null)
            {
                TempData["Success"] = resultado.Error;
                return RedirectToAction("Index", "Cart");
            }

            TempData["Success"] = "Pedido confirmado.";
            return RedirectToAction(nameof(Details), new { id = resultado.Pedido.Id });
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
