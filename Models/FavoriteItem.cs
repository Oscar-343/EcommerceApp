using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Favoritos: productos o rutas guardados por usuario.
    public class FavoriteItem
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        // Tipo indica si es producto o servicio.
        [MaxLength(20)]
        public string Type { get; set; } = "Product"; // Product o Service

        public int ItemId { get; set; } // Id de Product o Service

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación opcional
        public ApplicationUser? User { get; set; }
    }
}