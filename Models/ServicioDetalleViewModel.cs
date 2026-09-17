namespace EcommerceApp.Models
{
    /// <summary>
    /// ViewModel para la página de detalle de una ruta/servicio.
    /// </summary>
    public class ServicioDetalleViewModel
    {
        // Ruta principal
        public Service Service { get; set; } = new();

        // Guía principal (si existe)
        public Guide? Guide { get; set; }

        // Transporte (si existe)
        public Transport? Transport { get; set; }

        // Galería de imágenes (parseadas desde Service.GalleryImages)
        public List<string> GalleryImagesList { get; set; } = new();

        // Lista de qué incluye (parseada desde Service.Includes)
        public List<string> IncludesList { get; set; } = new();

        // Lista de qué no incluye (parseada desde Service.Excludes)
        public List<string> ExcludesList { get; set; } = new();

        // Recomendaciones (parseadas desde Service.Recommendations)
        public List<string> RecommendationsList { get; set; } = new();

        // Productos recomendados para esta ruta
        public List<Product> RecommendedProducts { get; set; } = new();

        // Información de dificultad
        public string DifficultyLabel { get; set; } = string.Empty;
        public string DifficultyColor { get; set; } = string.Empty;
        public string DifficultyEmoji { get; set; } = string.Empty;

        // ¿Tiene transporte?
        public bool HasTransport => Transport != null;

        // ¿Tiene guía?
        public bool HasGuide => Guide != null;

        // ¿Está disponible para reservar?
        public bool IsAvailable => Service.Status == "Active";
    }
}
