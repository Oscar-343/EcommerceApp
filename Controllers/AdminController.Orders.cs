using EcommerceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    // Sección Pedidos del panel de administración.
    public partial class AdminController
    {
        // ---------------------------------------------------------------
        // PEDIDOS
        // ---------------------------------------------------------------

        public async Task<IActionResult> Orders(string? status)
        {
            ViewData["Title"] = "Pedidos";
            ViewData["Subtitle"] = "Pedidos hechos por los usuarios desde el carrito";

            var query = context.Orders.AsNoTracking()
                .Include(o => o.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            ViewBag.StatusFilter = status;
            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            ViewData["Title"] = $"Pedido #{id}";
            ViewData["Subtitle"] = "Pedidos";

            var order = await context.Orders.AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // Cambia el estado de un pedido validando la transición (no cualquier estado vale desde cualquier otro).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OrderUpdateStatus(int id, string status)
        {
            var order = await context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            var transicionValida = order.Status == "Pendiente" && (status == "Entregado" || status == "Cancelado");
            if (!transicionValida)
            {
                TempData["Success"] = "Transición de estado no permitida.";
                return RedirectToAction(nameof(OrderDetails), new { id });
            }

            await using var transaction = await context.Database.BeginTransactionAsync();

            if (status == "Entregado")
            {
                order.DeliveredAt = DateTime.UtcNow;
            }
            else if (status == "Cancelado")
            {
                // Devolver el stock de cada línea cuyo producto siga existiendo.
                foreach (var item in order.Items.Where(i => i.ProductId != null))
                {
                    // Suma sobre el valor actual en la BD, no sobre uno leído antes.
                    await context.Products
                        .Where(p => p.Id == item.ProductId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity));
                }
            }

            order.Status = status;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["Success"] = "Estado del pedido actualizado.";
            return RedirectToAction(nameof(OrderDetails), new { id });
        }
    }
}
