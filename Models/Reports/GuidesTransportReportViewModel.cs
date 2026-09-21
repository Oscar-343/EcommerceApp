namespace EcommerceApp.Models.Reports
{
    // Reporte de guías y transportes: rutas asignadas (estado actual) y reservas Acabado por
    // guía, filtradas por fecha de salida (TripDate).
    public class GuidesTransportReportViewModel
    {
        public DateOnly Desde { get; set; }
        public DateOnly Hasta { get; set; }

        public List<GuideRouteRow> RutasPorGuia { get; set; } = new();
        public List<TransportRouteRow> RutasPorTransporte { get; set; } = new();
        public List<GuideReservationRow> ReservasPorGuia { get; set; } = new();
    }

    public class GuideRouteRow
    {
        public string GuideName { get; set; } = string.Empty;
        public int CantidadRutas { get; set; }
    }

    public class TransportRouteRow
    {
        public string TransportName { get; set; } = string.Empty;
        public int CantidadRutas { get; set; }
    }

    public class GuideReservationRow
    {
        public string GuideName { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public int Personas { get; set; }
        public decimal Monto { get; set; }
    }
}
