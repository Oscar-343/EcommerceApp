namespace EcommerceApp.Models
{
    /// <summary>
    /// ViewModel principal para el Home de la plataforma.
    /// </summary>
    public class HomeViewModel
    {
        // Rutas destacadas para la sección "Descubre tu próximo camino"
        public List<Service> FeaturedRoutes { get; set; } = new();

        // Productos seleccionados para "Equípate"
        public List<Product> FeaturedProducts { get; set; } = new();

        // Estadísticas dinámicas
        public int TotalRoutes { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }

        // Marcadores para el mapa (coordenadas de rutas)
        public List<MapMarker> RouteMarkers { get; set; } = new();

        // Publicaciones de comunidad (placeholder para futuro)
        public List<CommunityPost> CommunityPosts { get; set; } = new();

        // Información del usuario (si está autenticado)
        public bool IsAuthenticated { get; set; }
        public string? UserName { get; set; }
    }

    /// <summary>
    /// Marcador para el mapa de rutas.
    /// </summary>
    public class MapMarker
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Difficulty { get; set; }
        public double? DurationHours { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    /// <summary>
    /// Publicación de comunidad (estructura preparada para futuro).
    /// </summary>
    public class CommunityPost
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? UserAvatar { get; set; }
        public string? RouteName { get; set; }
        public string? Location { get; set; }
        public string? ImageUrl { get; set; }
    }
}