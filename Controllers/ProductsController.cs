using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using System.Security.Claims;

namespace EcommerceApp.Controllers
{
    // Tienda pública: catálogo de productos en solo lectura.
    // El CRUD se maneja desde el panel de administración (AdminController).
    public class ProductsController(ApplicationDbContext context) : Controller
    {
        // Experiencia mejorada de productos: Hero + categorías + catálogo con filtros.
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? category = null,
            string? search = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? brand = null,
            bool onlyOffers = false,
            string? orderBy = "featured")
        {
            // Consulta base: todos los productos
            var productsQuery = context.Products.AsNoTracking();

            // Filtro por categoría
            if (!string.IsNullOrEmpty(category))
            {
                productsQuery = productsQuery.Where(p => p.Category == category);
            }

            // Filtro por ofertas (precio promocional vigente)
            if (onlyOffers)
            {
                productsQuery = productsQuery.Where(p => p.PromotionalPrice != null && p.PromotionalPrice < p.Price);
            }

            // Filtro por búsqueda
            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                productsQuery = productsQuery.Where(p =>
                    p.Name.ToLower().Contains(searchLower) ||
                    p.Description.ToLower().Contains(searchLower) ||
                    (p.Brand != null && p.Brand.ToLower().Contains(searchLower)));
            }

            // Filtro por marca
            if (!string.IsNullOrEmpty(brand))
            {
                productsQuery = productsQuery.Where(p => p.Brand == brand);
            }

            // Filtro por rango de precios
            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p =>
                    (p.PromotionalPrice.HasValue && p.PromotionalPrice >= minPrice) ||
                    (!p.PromotionalPrice.HasValue && p.Price >= minPrice));
            }

            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p =>
                    (p.PromotionalPrice.HasValue && p.PromotionalPrice <= maxPrice) ||
                    (!p.PromotionalPrice.HasValue && p.Price <= maxPrice));
            }

            // Obtener total antes de aplicar orden
            var totalProducts = await productsQuery.CountAsync();

            // Aplicar ordenamiento
            productsQuery = orderBy switch
            {
                "price-asc" => productsQuery.OrderBy(p => p.PromotionalPrice ?? p.Price),
                "price-desc" => productsQuery.OrderByDescending(p => p.PromotionalPrice ?? p.Price),
                "newest" => productsQuery.OrderByDescending(p => p.CreatedAt),
                _ => productsQuery.OrderByDescending(p => p.IsFeatured)
                    .ThenByDescending(p => p.IsBestSeller)
                    .ThenByDescending(p => p.CreatedAt) // "featured" es el default
            };

            var products = await productsQuery.ToListAsync();

            // Obtener categorías y marcas disponibles para los filtros
            var allCategories = await context.Products.AsNoTracking()
                .Where(p => p.Category != null)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var allBrands = await context.Products.AsNoTracking()
                .Where(p => p.Brand != null)
                .Select(p => p.Brand)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            // Calcular el precio máximo en la tienda
            var maxPriceInStore = await context.Products.AsNoTracking()
                .Select(p => (decimal?)(p.PromotionalPrice ?? p.Price))
                .MaxAsync() ?? 0;

            // Favoritos del usuario actual (para pintar el corazón activo/inactivo).
            var favoriteProductIds = new List<int>();
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
                favoriteProductIds = await context.FavoriteItems
                    .AsNoTracking()
                    .Where(f => f.UserId == userId && f.Type == "Product")
                    .Select(f => f.ItemId)
                    .ToListAsync();
            }

            var model = new ProductosViewModel
            {
                Products = products,
                FavoriteProductIds = favoriteProductIds,
                ActiveCategory = category,
                SearchTerm = search,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SelectedBrand = brand,
                OnlyOffers = onlyOffers,
                AvailableCategories = allCategories ?? new List<string>(),
                AvailableBrands = allBrands ?? new List<string>(),
                TotalProducts = totalProducts,
                MaxPriceInStore = maxPriceInStore,
                OrderBy = orderBy
            };

            return View(model);
        }

        // Detalle de un producto
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }
    }
}