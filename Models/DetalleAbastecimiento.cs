namespace EcommerceApp.Models
{
    // Línea de un abastecimiento. Igual que OrderItem, guarda el nombre del producto
    // para que el historial no cambie si el producto se borra después.
    public class DetalleAbastecimiento
    {
        public int Id { get; set; }

        public int AbastecimientoId { get; set; }

        // Nullable: si se borra el producto, esta línea conserva su nombre (SetNull).
        public int? ProductoId { get; set; }

        public string NombreProducto { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        // Precio de compra al proveedor (NO el precio de venta al cliente).
        public decimal CostoUnitario { get; set; }

        // Cantidad * CostoUnitario, calculado en el servidor.
        public decimal Subtotal { get; set; }

        // Navegación
        public Abastecimiento? Abastecimiento { get; set; }
        public Product? Producto { get; set; }
    }
}
