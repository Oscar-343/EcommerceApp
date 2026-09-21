namespace EcommerceApp.Models
{
    // Línea de un pedido. Guarda una foto del producto al momento de comprar,
    // para que el historial no cambie si el producto se edita o se borra después.
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        // Nullable: si se borra el producto, esta línea conserva su nombre (SetNull).
        public int? ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string? ProductCategory { get; set; }

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        // Navegación
        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}
