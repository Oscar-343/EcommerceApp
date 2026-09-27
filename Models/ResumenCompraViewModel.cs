namespace EcommerceApp.Models
{
    // Resumen lateral "Tu equipo": productos, subtotal y total, calculados siempre en el servidor.
    // Sin envío ni impuestos: Total = Subtotal.
    public class ResumenCompraViewModel
    {
        public List<ItemResumen> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public int CantidadProductos => Items.Sum(i => i.Cantidad);
    }

    // Una línea del resumen de compra (precio vigente del producto, leído de la BD).
    public class ItemResumen
    {
        public string Nombre { get; set; } = "";
        public string? ImagenUrl { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => PrecioUnitario * Cantidad;
    }
}
