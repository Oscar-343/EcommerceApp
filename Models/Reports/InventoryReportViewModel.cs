namespace EcommerceApp.Models.Reports
{
    // Reporte de inventario: sin filtros de fecha, foto del catálogo actual.
    public class InventoryReportViewModel
    {
        public List<Product> StockBajo { get; set; } = new();
        public List<Product> Agotados { get; set; } = new();
        public decimal ValorInventario { get; set; }

        public List<CategoryCountRow> PorCategoria { get; set; } = new();
        public List<BrandCountRow> PorMarca { get; set; } = new();

        public List<Product> EnOferta { get; set; } = new();
    }

    public class CategoryCountRow
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class BrandCountRow
    {
        public string Brand { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
