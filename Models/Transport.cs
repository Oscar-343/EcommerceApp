using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Transporte disponible para los traslados hacia las rutas.
    public class Transport
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150), RegularExpression(@"^[\w\s\-]+$", ErrorMessage = "Solo texto, números, espacios y guiones.")]
        public string Name { get; set; } = string.Empty;

        // Tipo de vehículo: mini van, 4x4, colectivo...
        [MaxLength(100)]
        public string? Type { get; set; }

        // Cantidad de asientos.
        [Range(1, 100, ErrorMessage = "La capacidad debe ser entre 1 y 100.")]
        public int Capacity { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "No puede ser negativo.")]
        public decimal? PricePerKm { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}