using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using System.Diagnostics;

namespace EcommerceApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
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

            // Categorías outdoor
            var outdoorCategories = RouteCategories.All.ToList();

            // Marcadores para el mapa (solo coordenadas)
            var routeMarkers = await _context.Services.AsNoTracking()
                .Where(s => s.Status == "Active" && (s.StartLatitude.HasValue || s.Location != null))
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
                .Take(10)
                .ToListAsync();

            // Publicaciones de comunidad (placeholder vacío por ahora)
            var communityPosts = new List<CommunityPost>
            {
                new CommunityPost
                {
                    UserName = "@montañista",
                    RouteName = "Parque Nacional Tunari",
                    Location = "Cochabamba",
                    ImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=400&h=400&fit=crop"
                },
                new CommunityPost
                {
                    UserName = "@trekker",
                    RouteName = "Choro Trek",
                    Location = "La Paz",
                    ImageUrl = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=400&h=400&fit=crop"
                },
                new CommunityPost
                {
                    UserName = "@explorador",
                    RouteName = "Camino Inca",
                    Location = "Perú",
                    ImageUrl = "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=400&h=400&fit=crop"
                }
            };

            var model = new HomeViewModel
            {
                FeaturedRoutes = featuredRoutes,
                FeaturedProducts = featuredProducts,
                TotalRoutes = totalRoutes,
                TotalProducts = totalProducts,
                TotalCategories = totalCategories,
                OutdoorCategories = outdoorCategories,
                RouteMarkers = routeMarkers,
                CommunityPosts = communityPosts,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                UserName = User.Identity?.Name
            };

            return View(model);
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
