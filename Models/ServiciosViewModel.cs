namespace EcommerceApp.Models
{
    /// <summary>
    /// ViewModel para la página principal de Servicios/Rutas.
    /// </summary>
    public class ServiciosViewModel
    {
        // Rutas a mostrar
        public List<Service> Services { get; set; } = new();

        // Región activa (filtro)
        public string? ActiveRegion { get; set; }

        // Dificultad activa (filtro)
        public string? ActiveDifficulty { get; set; }

        // Categoría activa (filtro)
        public string? ActiveCategory { get; set; }

        // Duración mínima y máxima en horas (filtro por rango)
        public double? MinDuration { get; set; }
        public double? MaxDuration { get; set; }

        // Búsqueda
        public string? SearchTerm { get; set; }

        // Estadísticas
        public int TotalRoutes { get; set; }
        public int TotalDifficulties { get; set; }
        public int TotalWithGuide { get; set; }

        // Categorías disponibles
        public List<string> AvailableCategories { get; set; } = new();

        // Regiones disponibles
        public List<string> AvailableRegions { get; set; } = new();

        // Orden elegido: null (destacadas), price-asc, price-desc, duration, distance
        public string? OrderBy { get; set; }

        // Paginación
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalFiltered { get; set; }

        // Todas las rutas filtradas que tienen coordenadas (para el mapa)
        public List<RutaMapaItem> MapRoutes { get; set; } = new();

        // Rutas que el usuario ya guardó en favoritos
        public List<int> FavoriteServiceIds { get; set; } = new();
    }

    // Punto del mapa de rutas: solo lo necesario para el marcador y su popup.
    public record RutaMapaItem(int Id, string Name, string? Location, double Lat, double Lng);
}
