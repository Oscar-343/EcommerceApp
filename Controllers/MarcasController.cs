using EcommerceApp.Models.Inventario;
using EcommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApp.Controllers
{
    // Panel admin: marcas de los productos. Las marcas no se borran, se desactivan.
    [Authorize(Roles = "Admin")]
    public class MarcasController(MarcaService marcas, IImageStorageService imageStorage) : Controller
    {
        // Mismas reglas de imagen que los productos en AdminController.
        private static readonly string[] TiposImagenPermitidos = { "image/jpeg", "image/png", "image/webp", "image/gif" };
        private const long MaxBytesImagen = 5 * 1024 * 1024;   // 5 MB

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Marcas";
            ViewData["Subtitle"] = "Marcas que se venden en la tienda";
            return View(await marcas.ListarResumenAsync());
        }

        public IActionResult Crear()
        {
            ViewData["Title"] = "Nueva marca";
            ViewData["Subtitle"] = "Marcas";
            return View(new MarcaFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Crear(MarcaFormViewModel model, IFormFile? logoFile)
        {
            ViewData["Title"] = "Nueva marca";
            ViewData["Subtitle"] = "Marcas";

            // Validar primero: si el formulario tiene errores, no se sube nada a Supabase.
            if (!ModelState.IsValid)
                return View(model);

            var error = await SubirLogoAsync(model, logoFile);
            if (error != null)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            var resultado = await marcas.CrearAsync(model.Nombre, model.LogoUrl);
            if (!resultado.Exito)
            {
                ModelState.AddModelError(nameof(model.Nombre), resultado.Error!);
                return View(model);
            }

            TempData["Success"] = $"Marca \"{model.Nombre.Trim()}\" creada.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Editar(int id)
        {
            var marca = await marcas.ObtenerAsync(id);
            if (marca == null) return NotFound();

            ViewData["Title"] = "Editar marca";
            ViewData["Subtitle"] = "Marcas";
            return View(new MarcaFormViewModel { Id = marca.Id, Nombre = marca.Nombre, LogoUrl = marca.LogoUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Editar(int id, MarcaFormViewModel model, IFormFile? logoFile)
        {
            ViewData["Title"] = "Editar marca";
            ViewData["Subtitle"] = "Marcas";

            if (model.Id != id) return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var error = await SubirLogoAsync(model, logoFile);
            if (error != null)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            var resultado = await marcas.ActualizarAsync(id, model.Nombre, model.LogoUrl);
            if (!resultado.Exito)
            {
                ModelState.AddModelError(nameof(model.Nombre), resultado.Error!);
                return View(model);
            }

            TempData["Success"] = $"Marca \"{model.Nombre.Trim()}\" actualizada.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, bool activo)
        {
            var resultado = await marcas.CambiarEstadoAsync(id, activo);
            if (resultado.Exito)
                TempData["Success"] = activo ? "Marca activada." : "Marca desactivada.";
            else
                TempData["Error"] = resultado.Error;   // ej.: todavía tiene productos

            return RedirectToAction(nameof(Index));
        }

        // Si llegó un archivo de logo, lo sube a Supabase y deja la URL en el modelo.
        // Devuelve el mensaje de error, o null si todo salió bien (o no había archivo).
        private async Task<string?> SubirLogoAsync(MarcaFormViewModel model, IFormFile? logoFile)
        {
            if (logoFile == null || logoFile.Length == 0)
                return null;

            if (!TiposImagenPermitidos.Contains(logoFile.ContentType, StringComparer.OrdinalIgnoreCase))
                return $"\"{logoFile.FileName}\" no es una imagen válida. Usa JPG, PNG, WEBP o GIF.";

            if (logoFile.Length > MaxBytesImagen)
                return $"El logo \"{logoFile.FileName}\" supera el máximo de 5 MB.";

            try
            {
                await using var stream = logoFile.OpenReadStream();
                model.LogoUrl = await imageStorage.UploadAsync(stream, logoFile.FileName, logoFile.ContentType, "brands");
                return null;
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }
    }
}
