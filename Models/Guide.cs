using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Guía de montaña que acompaña a los grupos en las rutas.
    public class Guide
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        // Especialidad: senderismo, trekking, alta montaña...
        [MaxLength(200)]
        public string? Specialty { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [EmailAddress, MaxLength(150)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}