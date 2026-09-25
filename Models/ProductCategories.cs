namespace EcommerceApp.Models
{
    // Categorías canónicas de la tienda (lista fija en código, sin tabla en BD).
    // Coinciden con las categorías que usa ProductSeeder. Se usan en el navbar,
    // en el formulario del panel admin y para filtrar productos.
    public static class ProductCategories
    {
        public static readonly string[] All =
        {
            "Mochilas",               // recuadro grande
            "Calzado de trekking",    // ancho
            "Tiendas de campaña",     // alto
            "Ropa outdoor",
            "Bastones de trekking",
            "Camping",
            "Hidratación",
            "Iluminación",
            "Seguridad y orientación",
            "Accesorios"
        };

        // Imagen de portada para cada categoría (vista Products/Index).
        public static readonly Dictionary<string, string> Images = new()
        {
            { "Tiendas de campaña", "https://plus.unsplash.com/premium_photo-1669047973007-fd5bfbeabe4c?w=600&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MXx8dGllbmRhcyUyMGRlJTIwY2FtcGElQzMlQjFhJTIwc2VuZGVyaXNtb3xlbnwwfHwwfHx8MA%3D%3D" },
            { "Calzado de trekking", "https://images.unsplash.com/photo-1573543794198-73ff121c0a8f?w=600&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8OHx8Ym90YXMlMjB0cmVra2luZ3xlbnwwfHwwfHx8MA%3D%3D" },
            { "Mochilas", "https://images.unsplash.com/photo-1537430802614-118bf14be50c?w=600&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8N3x8bW9jaGlsYXMlMjBhdmVudHVyYXN8ZW58MHx8MHx8fDA%3D" },
            { "Bastones de trekking", "https://images.unsplash.com/photo-1632411316785-33d395035a3c?w=600&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTB8fGJhc3RvbmVzJTIwdHJla2tpbmd8ZW58MHx8MHx8fDA%3D" },
            { "Ropa outdoor", "https://images.unsplash.com/photo-1493568000180-ca2fb70ddcba?w=600&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8cm9wYSUyMHNlbmRlcmlzbW98ZW58MHx8MHx8fDA%3D" },
            { "Camping", "https://images.unsplash.com/photo-1504280390367-361c6d9f38f4?w=600&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Mnx8Y2FtcGluZ3xlbnwwfHwwfHx8MA%3D%3D" },
            { "Hidratación", "https://images.unsplash.com/photo-1558169550-45825435a09b?w=600&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8MTJ8fGhpZHJhdGFjaW9uJTIwc2VuZGVyaXNtb3xlbnwwfHwwfHx8MA%3D%3D" },
            { "Iluminación", "https://images.unsplash.com/photo-1600638765052-d3e31481af61?w=600&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8bGludGVybmFzJTIwc2VuZGVyaXNtb3xlbnwwfHwwfHx8MA%3D%3D" },
            { "Accesorios", "https://images.unsplash.com/photo-1476979735039-2fdea9e9e407?w=600&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8M3x8YWNjZXNvcmlvcyUyMHNlbmRlcmlzbW98ZW58MHx8MHx8fDA%3D" },
            { "Seguridad y orientación", "https://images.unsplash.com/photo-1588869222183-9269c9a5d330?w=600&auto=format&fit=crop&q=60&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxzZWFyY2h8Nnx8Y3VlcmRhcyUyMGVzY2FsYXJ8ZW58MHx8MHx8fDA%3D" }
        };

        // Imagen genérica cuando una categoría no tiene foto asignada.
        public const string DefaultImage = "https://images.unsplash.com/photo-1519904981063-b0cf448d479e?w=400&h=300&fit=crop";
    }
}