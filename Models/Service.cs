using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Servicio/ruta de senderismo y aventura.
    public class Service
    {
        [Key]
        public int Id { get; set; }

        // === INFORMACIÓN BÁSICA ===
        [Required, MaxLength(150), RegularExpression(@"^[\w\s\-]+$", ErrorMessage = "Solo texto, números, espacios y guiones.")]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? ShortDescription { get; set; }

        // === UBICACIÓN Y REGIÓN ===
        // Región: Cochabamba, Bolivia, Internacional
        [MaxLength(50)]
        public string? Region { get; set; }

        // Ubicación específica
        [MaxLength(150)]
        public string? Location { get; set; }

        // Punto de inicio (nombre del lugar)
        [MaxLength(200)]
        public string? StartPoint { get; set; }

        // Punto final/destino
        [MaxLength(200)]
        public string? EndPoint { get; set; }

        // === DIFICULTAD ===
        // easy, moderate, difficult, extreme
        [MaxLength(20), RegularExpression(@"^(easy|moderate|difficult|extreme)?$", ErrorMessage = "Dificultad no válida.")]
        public string? Difficulty { get; set; }

        // Descripción/explicación de la dificultad
        [MaxLength(500)]
        public string? DifficultyDescription { get; set; }

        // === DISTANCIA Y DURACIÓN ===
        [Range(1, 1000, ErrorMessage = "La distancia debe estar entre 1 y 1000 km.")]
        public double? DistanceKm { get; set; }

        // Duración en horas (número)
        public double? DurationHours { get; set; }

        // Texto de duración (ej: "6 horas", "2 días")
        [MaxLength(50)]
        public string? Duration { get; set; }

        // === GRUPO ===
        public int MaxGroupSize { get; set; } = 12;

        // === PRECIO ===
        [Required, Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal Price { get; set; }

        [MaxLength(100)]
        public string? PriceDescription { get; set; } // ej: "Bs. 150 / persona"

        // === IMÁGENES ===
        public string? ImageUrl { get; set; }

        // Galería de imágenes (separadas por pipe | )
        [MaxLength(2000)]
        public string? GalleryImages { get; set; }

        // Segunda imagen para uso en cards
        public string? SecondaryImageUrl { get; set; }

        // === ESTADO ===
        [MaxLength(20)]
        public string? Status { get; set; } = "Active";

        // Destacada en la home
        public bool IsFeatured { get; set; }

        // === QUÉ INCLUYE / NO INCLUYE ===
        [MaxLength(1000)]
        public string? Includes { get; set; } // Separado por pipe |

        [MaxLength(1000)]
        public string? Excludes { get; set; } // Separado por pipe |

        // === RECOMENDACIONES ANTES DE SALIR ===
        [MaxLength(2000)]
        public string? Recommendations { get; set; } // Separado por pipe |

        // === GEOGRAFÍA (para mapa) ===
        public double? StartLatitude { get; set; }
        public double? StartLongitude { get; set; }
        public double? EndLatitude { get; set; }
        public double? EndLongitude { get; set; }

        // === RELACIONES ===
        // Guía principal
        public int? GuideId { get; set; }
        public Guide? Guide { get; set; }

        // Vehículo de transporte (opcional)
        public int? TransportId { get; set; }
        public Transport? Transport { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    // Categorías de rutas predefinidas
    public static class RouteCategories
    {
        public static readonly string[] All =
        {
            "Senderismo",
            "Montañismo",
            "Bosque",
            "Trekking",
            "Escalada",
            "Alta montaña",
            "Miradores naturales",
            "Caminatas de naturaleza",
            "Volcanes",
            "Trekking con camping"
        };
    }

    // Niveles de dificultad
    public static class RouteDifficulty
    {
        public const string Easy = "easy";
        public const string Moderate = "moderate";
        public const string Difficult = "difficult";
        public const string Extreme = "extreme";

        public static readonly Dictionary<string, string> Labels = new()
        {
            { Easy, "Fácil" },
            { Moderate, "Moderada" },
            { Difficult, "Difícil" },
            { Extreme, "Extrema" }
        };

        public static readonly Dictionary<string, string> Colors = new()
        {
            { Easy, "#4CAF50" },      // Verde
            { Moderate, "#FFC107" },  // Amarillo
            { Difficult, "#F44336" }, // Rojo
            { Extreme, "#212121" }    // Negro
        };
    }

    // Regiones disponibles
    public static class RouteRegions
    {
        public static readonly string[] All =
        {
            "Cochabamba",
            "Bolivia",
            "Internacional"
        };
    }
}