using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;
using System.Diagnostics;
using System.Net;

namespace EcommerceApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, IEmailSender emailSender,
            IConfiguration configuration, ILogger<HomeController> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _configuration = configuration;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            // Obtener rutas destacadas (máximo 4)
            var featuredRoutes = await _context.Services.AsNoTracking()
                .Where(s => s.Status == "Active")
                .OrderByDescending(s => s.IsFeatured)
                .ThenByDescending(s => s.CreatedAt)
                .Take(4)
                .ToListAsync();

            // Obtener productos destacados (máximo 4)
            var featuredProducts = await _context.Products.AsNoTracking()
                .Include(p => p.Marca)
                .Where(p => p.Stock > 0)
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.IsBestSeller)
                .ThenByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            // Estadísticas dinámicas
            var totalRoutes = await _context.Services.AsNoTracking()
                .Where(s => s.Status == "Active")
                .CountAsync();

            var totalProducts = await _context.Products.AsNoTracking()
                .CountAsync();

            var totalCategories = await _context.Services.AsNoTracking()
                .Where(s => s.Status == "Active" && s.Category != null)
                .Select(s => s.Category)
                .Distinct()
                .CountAsync();

            // Marcadores del mapa: solo rutas que tienen coordenadas de inicio.
            var routeMarkers = await _context.Services.AsNoTracking()
                .Where(s => s.Status == "Active" && s.StartLatitude.HasValue && s.StartLongitude.HasValue)
                .Select(s => new MapMarker
                {
                    Id = s.Id,
                    Name = s.Name,
                    Location = s.Location,
                    Difficulty = s.Difficulty,
                    DurationHours = s.DurationHours,
                    Latitude = s.StartLatitude,
                    Longitude = s.StartLongitude
                })
                .ToListAsync();

            var model = new HomeViewModel
            {
                FeaturedRoutes = featuredRoutes,
                FeaturedProducts = featuredProducts,
                TotalRoutes = totalRoutes,
                TotalProducts = totalProducts,
                TotalCategories = totalCategories,
                RouteMarkers = routeMarkers,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                UserName = User.Identity?.Name
            };

            return View(model);
        }

        // Página "Nosotros": quiénes somos, con cifras reales del catálogo.
        [AllowAnonymous]
        public async Task<IActionResult> Nosotros()
        {
            var model = new NosotrosViewModel
            {
                TotalRutas = await _context.Services.CountAsync(s => s.Status == "Active"),
                TotalProductos = await _context.Products.CountAsync(),
                TotalGuias = await _context.Guides.CountAsync(g => g.IsActive),
                TotalRegiones = await _context.Services
                    .Where(s => s.Status == "Active" && s.Region != null)
                    .Select(s => s.Region)
                    .Distinct()
                    .CountAsync()
            };

            ViewData["Title"] = "Nosotros";
            return View(model);
        }

        // Página "Contacto": formulario + datos de contacto.
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Contacto()
        {
            ViewData["Title"] = "Contacto";
            return View(new ContactoViewModel());
        }

        // Envía el mensaje del formulario al correo de la tienda (SMTP o consola, ver SmtpEmailSender).
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contacto(ContactoViewModel model)
        {
            ViewData["Title"] = "Contacto";

            if (!ContactoViewModel.Motivos.Contains(model.Motivo))
                ModelState.AddModelError(nameof(model.Motivo), "Elige un motivo de la lista.");

            if (!ModelState.IsValid) return View(model);

            // Todo lo que escribió el usuario se codifica: el correo es HTML y no debe interpretar su texto.
            var cuerpo =
                $"<p><strong>Nombre:</strong> {WebUtility.HtmlEncode(model.Nombre)}</p>" +
                $"<p><strong>Correo:</strong> {WebUtility.HtmlEncode(model.Email)}</p>" +
                $"<p><strong>Teléfono:</strong> {WebUtility.HtmlEncode(model.Telefono ?? "—")}</p>" +
                $"<p><strong>Motivo:</strong> {WebUtility.HtmlEncode(model.Motivo)}</p>" +
                $"<p><strong>Mensaje:</strong><br/>{WebUtility.HtmlEncode(model.Mensaje).Replace("\n", "<br/>")}</p>";

            var destino = _configuration["Contact:Email"] ?? "trenalsur@gmail.com";

            try
            {
                await _emailSender.SendEmailAsync(destino, $"Contacto web — {model.Motivo}", cuerpo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el mensaje de contacto.");
                ModelState.AddModelError(string.Empty, "No pudimos enviar tu mensaje ahora. Inténtalo más tarde o escríbenos por WhatsApp.");
                return View(model);
            }

            // Post → Redirect → Get: al recargar no se reenvía el formulario.
            TempData["Success"] = "¡Gracias! Recibimos tu mensaje y te responderemos pronto.";
            return RedirectToAction(nameof(Contacto));
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
