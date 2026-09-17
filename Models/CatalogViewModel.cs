namespace EcommerceApp.Models
{
    // Modelo combinado de la tienda: productos (destacados o filtrados) + servicios/rutas.
    public class CatalogViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Service> Services { get; set; } = new();

        // Categoría activa cuando se filtra el catálogo (null en la portada general).
        public string? Category { get; set; }
    }
}