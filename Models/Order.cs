using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Pedido: nace cuando un usuario confirma su carrito.
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Estado: Pendiente, Entregado, Cancelado. Ambos finales excepto Pendiente.
        [MaxLength(20), RegularExpression(@"^(Pendiente|Entregado|Cancelado)$", ErrorMessage = "Estado no válido.")]
        public string Status { get; set; } = "Pendiente";

        // Total = suma de las líneas, calculado en el servidor al confirmar el pedido.
        public decimal Total { get; set; }

        // Fecha en la que se marcó como Entregado (base del ingreso en los reportes).
        public DateTime? DeliveredAt { get; set; }

        // Navegación
        public ApplicationUser? User { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
