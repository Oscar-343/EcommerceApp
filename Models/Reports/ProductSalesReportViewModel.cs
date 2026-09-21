namespace EcommerceApp.Models.Reports
{
    // Reporte de ventas: pedidos Entregado en el rango, por DeliveredAt.
    public class ProductSalesReportViewModel
    {
        public DateOnly Desde { get; set; }
        public DateOnly Hasta { get; set; }

        public List<ProductSalesRow> PorProducto { get; set; } = new();
        public List<CategorySalesRow> PorCategoria { get; set; } = new();

        public int TotalUnidades { get; set; }
        public decimal TotalMonto { get; set; }
    }

    public class ProductSalesRow
    {
        // Nombre del snapshot (OrderItem.ProductName), no del producto actual.
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }

    public class CategorySalesRow
    {
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }
}
