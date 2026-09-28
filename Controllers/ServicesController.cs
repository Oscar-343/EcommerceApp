using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    // Plataforma de experiencias: catálogo de rutas de senderismo y aventura.
    // El CRUD se maneja desde el panel de administración (AdminController).
    public class ServicesController(ApplicationDbContext context) : Controller
    {
        // Página principal de Servicios/Rutas
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? region = null,
            string? difficulty = null,
            string? category = null,
            double? minDuration = null,
            double? maxDuration = null,
            string? search = null,
            string? orderBy = null,
            int page = 1)
        {
            // Consulta base: todas las rutas activas
            var servicesQuery = context.Services.AsNoTracking()
                .Where(s => s.Status == "Active");

            // Filtro por región
            if (!string.IsNullOrEmpty(region))
            {
                servicesQuery = servicesQuery.Where(s => s.Region == region);
            }

            // Filtro por dificultad
            if (!string.IsNullOrEmpty(difficulty))
            {
                servicesQuery = servicesQuery.Where(s => s.Difficulty == difficulty);
            }

            // Filtro por categoría
            if (!string.IsNullOrEmpty(category))
            {
                servicesQuery = servicesQuery.Where(s => s.Category == category);
            }

            // Filtro por rango de duración
            if (minDuration.HasValue)
            {
                servicesQuery = servicesQuery.Where(s =>
                    s.DurationHours.HasValue && s.DurationHours >= minDuration);
            }
            if (maxDuration.HasValue)
            {
                servicesQuery = servicesQuery.Where(s =>
                    s.DurationHours.HasValue && s.DurationHours <= maxDuration);
            }

            // Filtro por búsqueda
            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                servicesQuery = servicesQuery.Where(s =>
                    s.Name.ToLower().Contains(searchLower) ||
                    s.Description.ToLower().Contains(searchLower) ||
                    (s.Location != null && s.Location.ToLower().Contains(searchLower)));
            }

            // Orden elegido en "Ordenar por". Por defecto: destacadas primero, luego las más nuevas.
            servicesQuery = orderBy switch
            {
                "price-asc" => servicesQuery.OrderBy(s => s.Price),
                "price-desc" => servicesQuery.OrderByDescending(s => s.Price),
                "duration" => servicesQuery.OrderBy(s => s.DurationHours ?? double.MaxValue),
                "distance" => servicesQuery.OrderBy(s => s.DistanceKm ?? double.MaxValue),
                _ => servicesQuery.OrderByDescending(s => s.IsFeatured).ThenByDescending(s => s.CreatedAt)
            };

            // Paginación: 9 rutas por página (3 filas de 3 en escritorio).
            const int porPagina = 9;
            var totalFiltradas = await servicesQuery.CountAsync();
            var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalFiltradas / (double)porPagina));
            page = Math.Clamp(page, 1, totalPaginas);

            var services = await servicesQuery
                .Skip((page - 1) * porPagina)
                .Take(porPagina)
                .ToListAsync();

            // El mapa muestra todas las rutas filtradas (no solo las de la página actual).
            var mapRoutes = await servicesQuery
                .Where(s => s.StartLatitude != null && s.StartLongitude != null)
                .Select(s => new RutaMapaItem(s.Id, s.Name, s.Location, s.StartLatitude!.Value, s.StartLongitude!.Value))
                .ToListAsync();

            // Rutas favoritas del usuario (para pintar el corazón lleno).
            var favoriteServiceIds = new List<int>();
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
                favoriteServiceIds = await context.FavoriteItems.AsNoTracking()
                    .Where(f => f.UserId == userId && f.Type == "Service")
                    .Select(f => f.ItemId)
                    .ToListAsync();
            }

            // Obtener estadísticas
            var totalRoutes = await context.Services.AsNoTracking()
                .Where(s => s.Status == "Active")
                .CountAsync();

            var totalDifficulties = await context.Services.AsNoTracking()
                .Where(s => s.Status == "Active" && s.Difficulty != null)
                .Select(s => s.Difficulty)
                .Distinct()
                .CountAsync();

            var totalWithGuide = await context.Services.AsNoTracking()
                .Where(s => s.Status == "Active" && s.GuideId != null)
                .CountAsync();

            // Obtener categorías y regiones disponibles
            var availableCategories = await context.Services.AsNoTracking()
                .Where(s => s.Status == "Active" && s.Category != null)
                .Select(s => s.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var availableRegions = await context.Services.AsNoTracking()
                .Where(s => s.Status == "Active" && s.Region != null)
                .Select(s => s.Region)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();

            var model = new ServiciosViewModel
            {
                Services = services,
                ActiveRegion = region,
                ActiveDifficulty = difficulty,
                ActiveCategory = category,
                MinDuration = minDuration,
                MaxDuration = maxDuration,
                SearchTerm = search,
                TotalRoutes = totalRoutes,
                TotalDifficulties = totalDifficulties,
                TotalWithGuide = totalWithGuide,
                AvailableCategories = availableCategories,
                AvailableRegions = availableRegions,
                OrderBy = orderBy,
                Page = page,
                TotalPages = totalPaginas,
                TotalFiltered = totalFiltradas,
                MapRoutes = mapRoutes,
                FavoriteServiceIds = favoriteServiceIds
            };

            return View(model);
        }

        // Detalle de una ruta específica
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            // Cargar la ruta con sus relaciones
            var service = await context.Services.AsNoTracking()
                .Include(s => s.Guide)
                .Include(s => s.Transport)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
                return NotFound();

            // Parsear la galería de imágenes
            var galleryImages = new List<string>();
            if (!string.IsNullOrEmpty(service.GalleryImages))
            {
                galleryImages = service.GalleryImages
                    .Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(img => img.Trim())
                    .ToList();
            }

            // Parsear qué incluye
            var includesList = new List<string>();
            if (!string.IsNullOrEmpty(service.Includes))
            {
                includesList = service.Includes
                    .Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .ToList();
            }

            // Parsear qué no incluye
            var excludesList = new List<string>();
            if (!string.IsNullOrEmpty(service.Excludes))
            {
                excludesList = service.Excludes
                    .Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .ToList();
            }

            // Parsear recomendaciones
            var recommendationsList = new List<string>();
            if (!string.IsNullOrEmpty(service.Recommendations))
            {
                recommendationsList = service.Recommendations
                    .Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .ToList();
            }

            // Obtener información de dificultad
            var difficultyLabel = !string.IsNullOrEmpty(service.Difficulty) &&
                RouteDifficulty.Labels.ContainsKey(service.Difficulty)
                ? RouteDifficulty.Labels[service.Difficulty]
                : "No especificada";

            var difficultyColor = !string.IsNullOrEmpty(service.Difficulty) &&
                RouteDifficulty.Colors.ContainsKey(service.Difficulty)
                ? RouteDifficulty.Colors[service.Difficulty]
                : "#999";

            var difficultyEmoji = service.Difficulty switch
            {
                RouteDifficulty.Easy => "🟢",
                RouteDifficulty.Moderate => "🟡",
                RouteDifficulty.Difficult => "🔴",
                RouteDifficulty.Extreme => "⚫",
                _ => "⚪"
            };

            // Equipamiento recomendado: productos asignados a esta ruta (tabla ServiceProduct).
            var recommendedProducts = await context.ServiceProducts.AsNoTracking()
                .Include(sp => sp.Product)
                .Where(sp => sp.ServiceId == id && sp.Product != null && sp.Product.Stock > 0)
                .Select(sp => sp.Product!)
                .ToListAsync();

            // ¿El usuario ya guardó esta ruta en favoritos? (para pintar el corazón)
            var isFavorite = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
                isFavorite = await context.FavoriteItems.AnyAsync(f =>
                    f.UserId == userId && f.Type == "Service" && f.ItemId == id);
            }

            var model = new ServicioDetalleViewModel
            {
                IsFavorite = isFavorite,
                Service = service,
                Guide = service.Guide,
                Transport = service.Transport,
                GalleryImagesList = galleryImages,
                IncludesList = includesList,
                ExcludesList = excludesList,
                RecommendationsList = recommendationsList,
                RecommendedProducts = recommendedProducts,
                DifficultyLabel = difficultyLabel,
                DifficultyColor = difficultyColor,
                DifficultyEmoji = difficultyEmoji
            };

            return View(model);
        }
    }
}
