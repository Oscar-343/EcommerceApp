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

        // Datos del checkout. Nullable: los pedidos confirmados directo desde el carrito no los tienen.
        // Pago SIMULADO: de la tarjeta solo se guarda la marca y los últimos 4 dígitos, para mostrarlos.
        [MaxLength(20)] public string? MetodoPago { get; set; }         // Tarjeta | QR | Transferencia
        [MaxLength(20)] public string? TarjetaMarca { get; set; }       // VISA | MASTERCARD
        [MaxLength(4)] public string? TarjetaUltimos4 { get; set; }
        [MaxLength(40)] public string? CodigoTransaccion { get; set; }

        [MaxLength(100)] public string? NombreEntrega { get; set; }
        [MaxLength(30)] public string? TelefonoEntrega { get; set; }
        [MaxLength(256)] public string? EmailEntrega { get; set; }
        [MaxLength(100)] public string? CiudadEntrega { get; set; }
        [MaxLength(200)] public string? DireccionEntrega { get; set; }
        [MaxLength(300)] public string? ReferenciaEntrega { get; set; }

        // Navegación
        public ApplicationUser? User { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
