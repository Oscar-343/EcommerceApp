using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;

namespace EcommerceApp.Controllers
{
    // Checkout: datos de entrega + pago simulado + confirmación del pedido.
    // Exige login, igual que el carrito y los pedidos. La lógica vive en PedidoService e IPagoService.
    [Authorize]
    public class CheckoutController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        PedidoService pedidoService,
        IPagoService pagoService) : Controller
    {
        private static readonly string[] MarcasValidas = { "VISA", "MASTERCARD" };

        // Página del checkout. Sin carrito (o con problemas de stock) no se puede entrar.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var items = await pedidoService.ObtenerCarritoAsync(userId);

            var error = PedidoService.ValidarCarrito(items);
            if (error != null)
            {
                TempData["Success"] = error;
                return RedirectToAction("Index", "Cart");
            }

            // Prellena con lo que ya sabemos del usuario; el resto lo completa en el formulario.
            var user = await userManager.GetUserAsync(User);
            var model = new CheckoutViewModel
            {
                NombreCompleto = user?.FullName ?? "",
                Email = user?.Email ?? "",
                Telefono = user?.PhoneNumber ?? "",
                Direccion = user?.Address ?? "",
                Resumen = PedidoService.ArmarResumen(items)
            };

            ViewData["Title"] = "Checkout";
            return View(model);
        }

        // Procesa el pago simulado y crea el pedido. Post → Redirect → Get: al recargar la confirmación no se duplica.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pagar(CheckoutViewModel model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

            // 1. El total se recalcula siempre en el servidor, con los precios de la BD.
            var items = await pedidoService.ObtenerCarritoAsync(userId);
            var error = PedidoService.ValidarCarrito(items);
            if (error != null)
            {
                TempData["Success"] = error;
                return RedirectToAction("Index", "Cart");
            }
            model.Resumen = PedidoService.ArmarResumen(items);
            ViewData["Title"] = "Checkout";

            // 2. Datos de entrega inválidos → vuelve al formulario con los errores.
            if (!ModelState.IsValid) return View("Index", model);

            // 3. De la tarjeta solo se aceptan marca conocida y exactamente 4 dígitos; lo demás se descarta.
            if (model.MetodoPago == "Tarjeta")
            {
                if (!MarcasValidas.Contains(model.TarjetaMarca)) model.TarjetaMarca = null;
                if (model.TarjetaUltimos4 == null || !Regex.IsMatch(model.TarjetaUltimos4, @"^\d{4}$")) model.TarjetaUltimos4 = null;
            }
            else
            {
                model.TarjetaMarca = null;
                model.TarjetaUltimos4 = null;
            }

            // 4. Pago simulado (valida el método y el monto).
            var pago = await pagoService.ProcesarAsync(model.MetodoPago, model.Resumen.Total);
            if (!pago.Exitoso)
            {
                ModelState.AddModelError("", pago.Mensaje);
                return View("Index", model);
            }

            // 5. Crea el pedido (estado Pendiente) y vacía el carrito, con el mismo flujo de "Confirmar pedido".
            var datos = new DatosCheckout(
                model.NombreCompleto.Trim(), model.Telefono.Trim(), model.Email.Trim(), model.Ciudad.Trim(),
                model.Direccion.Trim(), string.IsNullOrWhiteSpace(model.Referencia) ? null : model.Referencia.Trim(),
                model.MetodoPago, model.TarjetaMarca, model.TarjetaUltimos4, pago.CodigoTransaccion);

            var resultado = await pedidoService.CrearDesdeCarritoAsync(userId, datos);
            if (resultado.Pedido == null)
            {
                // Otra compra se llevó el stock entre la revisión y el pago.
                TempData["Success"] = resultado.Error;
                return RedirectToAction("Index", "Cart");
            }

            return RedirectToAction(nameof(Confirmacion), new { id = resultado.Pedido.Id });
        }

        // Compra completada. Solo muestra pedidos del usuario actual; si no es suyo → 404.
        [HttpGet]
        public async Task<IActionResult> Confirmacion(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var order = await context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();

            ViewData["Title"] = "Pedido confirmado";
            return View(order);
        }
    }
}
