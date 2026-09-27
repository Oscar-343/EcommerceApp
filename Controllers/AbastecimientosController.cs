using EcommerceApp.Models.Inventario;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApp.Controllers
{
    // Panel admin: abastecer la tienda (suma stock) y consultar el historial.
    // No hay acciones de editar ni eliminar: un abastecimiento registrado no se modifica.
    [Authorize(Roles = "Admin")]
    public class AbastecimientosController(
        AbastecimientoService abastecimientos,
        ProveedorService proveedores,
        MarcaService marcas) : Controller
    {
        // Historial con filtros (fechas en hora Bolivia, formato yyyy-MM-dd).
        public async Task<IActionResult> Index(int? proveedorId, int? marcaId, DateOnly? desde, DateOnly? hasta)
        {
            ViewData["Title"] = "Abastecimientos";
            ViewData["Subtitle"] = "Historial de ingresos de mercadería";

            ViewBag.ProveedorId = proveedorId;
            ViewBag.MarcaId = marcaId;
            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
            // En el filtro van todos los proveedores: el historial de los inactivos sigue visible.
            ViewBag.Proveedores = await proveedores.ListarAsync(null, null, null);
            ViewBag.Marcas = await marcas.ListarAsync();

            return View(await abastecimientos.ListarAsync(proveedorId, marcaId, desde, hasta));
        }

        // Formulario "Abastecer tienda".
        // - proveedorId (desde la ficha del proveedor): viene preseleccionado.
        // - productoId (botón "Abastecer" de la lista de productos): el producto se agrega solo, y si un único
        //   proveedor activo distribuye su marca, ese proveedor viene preseleccionado.
        public async Task<IActionResult> Nuevo(int? proveedorId, int? productoId)
        {
            var model = new AbastecimientoFormViewModel { ProveedorId = proveedorId ?? 0 };

            if (productoId.HasValue)
            {
                ViewBag.ProductoInicial = productoId.Value;
                if (!proveedorId.HasValue)
                {
                    var candidatos = await abastecimientos.ProveedoresDeProductoAsync(productoId.Value);
                    if (candidatos.Count == 1)
                        model.ProveedorId = candidatos[0];
                    else if (candidatos.Count == 0)
                        ViewBag.AvisoProducto = "Ningún proveedor activo distribuye la marca de este producto. Asígnala a un proveedor primero.";
                }
            }

            return await MostrarFormulario(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Nuevo(AbastecimientoFormViewModel model)
        {
            if (!ModelState.IsValid)
                return await MostrarFormulario(model);

            // Quién registra sale del usuario autenticado, nunca del formulario.
            var usuario = User.Identity?.Name ?? "Admin";

            var resultado = await abastecimientos.RegistrarAsync(model, usuario);
            if (!resultado.Exito)
            {
                ModelState.AddModelError(string.Empty, resultado.Error!);
                return await MostrarFormulario(model);
            }

            var unidades = model.Lineas.Sum(l => l.Cantidad);
            TempData["Success"] = $"Abastecimiento registrado: se sumaron {unidades} unidades al stock de {model.Lineas.Count} producto(s).";
            return RedirectToAction(nameof(Detalle), new { id = resultado.Id });
        }

        // Comprobante del abastecimiento.
        public async Task<IActionResult> Detalle(int id)
        {
            var abastecimiento = await abastecimientos.ObtenerDetalleAsync(id);
            if (abastecimiento == null) return NotFound();

            ViewData["Title"] = $"Abastecimiento #{abastecimiento.Id:D4}";
            ViewData["Subtitle"] = "Abastecimientos";
            return View(abastecimiento);
        }

        // JSON para el formulario (AJAX): productos de las marcas que distribuye el proveedor.
        [HttpGet]
        public async Task<IActionResult> ProductosPorProveedor(int proveedorId)
        {
            var productos = await abastecimientos.ProductosPorProveedorAsync(proveedorId);

            return Json(productos.Select(p => new
            {
                id = p.Id,
                nombre = p.Name,
                marca = p.Marca?.Nombre,
                stock = p.Stock,
                stockMinimo = p.StockMinimo,
                imagenUrl = p.ImageUrl
            }));
        }

        // Muestra el formulario (al entrar o al volver con errores). Solo proveedores activos.
        private async Task<IActionResult> MostrarFormulario(AbastecimientoFormViewModel model)
        {
            ViewData["Title"] = "Abastecer tienda";
            ViewData["Subtitle"] = "Registra el equipo que llega a tu tienda";
            ViewBag.Proveedores = await proveedores.ListarActivosAsync();
            return View("Nuevo", model);
        }
    }
}
