namespace EcommerceApp.Models
{
    /// <summary>
    /// ViewModel para la sección Productos con filtros y búsqueda.
    /// </summary>
    public class ProductosViewModel
    {
        // Productos a mostrar en la vista
        public List<Product> Products { get; set; } = new();

        // Categoría activa (null si no hay filtro)
        public string? ActiveCategory { get; set; }

        // Búsqueda activa
        public string? SearchTerm { get; set; }

        // Rango de precios para filtro
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        // Marca seleccionada
        public string? SelectedBrand { get; set; }

        // Todas las categorías disponibles
        public List<string> AvailableCategories { get; set; } = new();

        // Todas las marcas disponibles
        public List<string> AvailableBrands { get; set; } = new();

        // Total de productos disponibles (sin filtro de página)
        public int TotalProducts { get; set; }

        // Precio máximo en la tienda (para rango de filtro)
        public decimal MaxPriceInStore { get; set; }

        // Ordenamiento actual: "featured", "newest", "price-asc", "price-desc"
        public string? OrderBy { get; set; } = "featured";
    }
}
