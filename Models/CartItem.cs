using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Carrito de compras por usuario (solo productos).
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public int ProductId { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        public decimal UnitPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public ApplicationUser? User { get; set; }
        public Product? Product { get; set; }
    }
}