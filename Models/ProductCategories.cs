namespace EcommerceApp.Models
{
    // Categorías canónicas de la tienda (lista fija en código, sin tabla en BD).
    // Se usan en el navbar, en el formulario del panel admin y para filtrar productos.
    public static class ProductCategories
    {
        public static readonly string[] All =
        {
            "Hombre",
            "Mujer",
            "Niños",
            "Ropa de abrigo",
            "Tiendas de campaña",
            "Equipo de cocina",
            "Bolsos y mochilas",
            "Equipamiento"
        };
    }
}