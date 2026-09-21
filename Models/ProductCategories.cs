namespace EcommerceApp.Models
{
    // Categorías canónicas de la tienda (lista fija en código, sin tabla en BD).
    // Coinciden con las categorías que usa ProductSeeder. Se usan en el navbar,
    // en el formulario del panel admin y para filtrar productos.
    public static class ProductCategories
    {
        public static readonly string[] All =
        {
            "Tiendas de campaña",
            "Calzado de trekking",
            "Mochilas",
            "Bastones de trekking",
            "Ropa outdoor",
            "Camping",
            "Hidratación",
            "Iluminación",
            "Accesorios",
            "Seguridad y orientación"
        };

        // Imagen de portada para cada categoría (vista Products/Index).
        public static readonly Dictionary<string, string> Images = new()
        {
            { "Tiendas de campaña", "https://images.unsplash.com/photo-1478131143081-80f7f84ca84d?w=400&h=300&fit=crop" },
            { "Calzado de trekking", "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=400&h=300&fit=crop" },
            { "Mochilas", "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=400&h=300&fit=crop" },
            { "Bastones de trekking", "https://images.unsplash.com/photo-1505228395891-9a51e7e86e81?w=400&h=300&fit=crop" },
            { "Ropa outdoor", "https://images.unsplash.com/photo-1533392839391-6db7ba1f2068?w=400&h=300&fit=crop" },
            { "Camping", "https://images.unsplash.com/photo-1571863533956-461c92f1704a?w=400&h=300&fit=crop" },
            { "Hidratación", "https://images.unsplash.com/photo-1535632066927-ab7c9ab60908?w=400&h=300&fit=crop" },
            { "Iluminación", "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&h=300&fit=crop" },
            { "Accesorios", "https://images.unsplash.com/photo-1606611282519-d91603135d4e?w=400&h=300&fit=crop" },
            { "Seguridad y orientación", "https://images.unsplash.com/photo-1577401132019-40a53b4fd6f0?w=400&h=300&fit=crop" }
        };

        // Imagen genérica cuando una categoría no tiene foto asignada.
        public const string DefaultImage = "https://images.unsplash.com/photo-1519904981063-b0cf448d479e?w=400&h=300&fit=crop";
    }
}