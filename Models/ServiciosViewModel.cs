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

        // Duración máxima en horas (filtro)
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
    }
}
