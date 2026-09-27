using EcommerceApp.Models.Inventario;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApp.Controllers
{
    // Panel admin: proveedores de la tienda. Controlador delgado: valida, llama al servicio y devuelve la vista.
    [Authorize(Roles = "Admin")]
    public class ProveedoresController(ProveedorService proveedores, MarcaService marcas) : Controller
    {
        // Listado con filtros. estado: "activos" (por defecto), "inactivos" o "todos".
        public async Task<IActionResult> Index(string? busqueda, int? marcaId, string estado = "activos")
        {
            ViewData["Title"] = "Proveedores";
            ViewData["Subtitle"] = "Quiénes equipan tu tienda";

            bool? activo = estado switch
            {
                "inactivos" => false,
                "todos" => null,
                _ => true
            };

            ViewBag.Busqueda = busqueda;
            ViewBag.MarcaId = marcaId;
            ViewBag.Estado = estado;
            ViewBag.Marcas = await marcas.ListarAsync();

            return View(await proveedores.ListarAsync(busqueda, marcaId, activo));
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var proveedor = await proveedores.ObtenerAsync(id);
            if (proveedor == null) return NotFound();

            ViewData["Title"] = proveedor.Nombre;
            ViewData["Subtitle"] = "Proveedores";
            return View(proveedor);
        }

        public async Task<IActionResult> Crear()
        {
            ViewData["Title"] = "Nuevo proveedor";
            ViewData["Subtitle"] = "Proveedores";

            var model = new ProveedorFormViewModel();
            await proveedores.CargarMarcasDisponiblesAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ProveedorFormViewModel model)
        {
            if (!ModelState.IsValid)
                return await VolverAlFormulario(model, "Nuevo proveedor");

            var resultado = await proveedores.CrearAsync(model);
            if (!resultado.Exito)
            {
                ModelState.AddModelError(string.Empty, resultado.Error!);
                return await VolverAlFormulario(model, "Nuevo proveedor");
            }

            TempData["Success"] = $"Proveedor \"{model.Nombre.Trim()}\" registrado.";
            return RedirectToAction(nameof(Detalle), new { id = resultado.Id });
        }

        public async Task<IActionResult> Editar(int id)
        {
            var model = await proveedores.ObtenerFormularioAsync(id);
            if (model == null) return NotFound();

            ViewData["Title"] = "Editar proveedor";
            ViewData["Subtitle"] = "Proveedores";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, ProveedorFormViewModel model)
        {
            if (model.Id != id) return NotFound();

            if (!ModelState.IsValid)
                return await VolverAlFormulario(model, "Editar proveedor");

            var resultado = await proveedores.ActualizarAsync(model);
            if (!resultado.Exito)
            {
                ModelState.AddModelError(string.Empty, resultado.Error!);
                return await VolverAlFormulario(model, "Editar proveedor");
            }

            TempData["Success"] = $"Proveedor \"{model.Nombre.Trim()}\" actualizado.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // Activar / desactivar (se llama desde el listado o desde la ficha; volverA indica a dónde regresar).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, bool activo, string? volverA)
        {
            var resultado = await proveedores.CambiarEstadoAsync(id, activo);
            if (resultado.Exito)
                TempData["Success"] = activo ? "Proveedor activado." : "Proveedor desactivado. Su historial sigue disponible.";
            else
                TempData["Error"] = resultado.Error;

            // Solo URLs del propio sitio, para no redirigir a páginas externas.
            if (!string.IsNullOrEmpty(volverA) && Url.IsLocalUrl(volverA))
                return LocalRedirect(volverA);

            return RedirectToAction(nameof(Index));
        }

        // Vuelve a mostrar el formulario con sus errores (los chips de marcas no viajan en el POST).
        private async Task<IActionResult> VolverAlFormulario(ProveedorFormViewModel model, string titulo)
        {
            ViewData["Title"] = titulo;
            ViewData["Subtitle"] = "Proveedores";
            await proveedores.CargarMarcasDisponiblesAsync(model);
            return View(model);
        }
    }
}
