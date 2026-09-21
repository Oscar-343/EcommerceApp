namespace EcommerceApp.Models.Reports
{
    // Reporte de demanda: favoritos guardados y presencia en carritos actuales. Sin filtro de fecha (foto actual).
    public class DemandReportViewModel
    {
        public List<FavoriteCountRow> ProductosFavoritos { get; set; } = new();
        public List<FavoriteCountRow> RutasFavoritas { get; set; } = new();

        // Carritos actuales: reflejan interés, no compras confirmadas.
        public List<CartDemandRow> ProductosEnCarrito { get; set; } = new();
    }

    public class FavoriteCountRow
    {
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class CartDemandRow
    {
        public string ProductName { get; set; } = string.Empty;
        public int CantidadTotal { get; set; }
        public int CarritosDistintos { get; set; }
    }
}
