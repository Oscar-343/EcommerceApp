namespace EcommerceApp.Models.Reports
{
    // Reporte de reservas: filtrado por fecha de salida (TripDate), estado y ruta.
    public class ReservationsReportViewModel
    {
        public DateOnly Desde { get; set; }
        public DateOnly Hasta { get; set; }
        public string? Estado { get; set; }
        public int? ServiceId { get; set; }

        // Para el <select> de rutas del filtro.
        public List<(int Id, string Name)> Rutas { get; set; } = new();

        public List<ReservationStatusRow> PorEstado { get; set; } = new();
        public double TasaCancelacion { get; set; }

        public List<ReservationRouteRow> PorRuta { get; set; } = new();
        public List<ReservationMonthRow> PorMes { get; set; } = new();
    }

    public class ReservationStatusRow
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class ReservationRouteRow
    {
        public string RouteName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int People { get; set; }
        // Monto: solo de reservas Acabado (ingreso real de esa ruta).
        public decimal AmountAcabado { get; set; }
    }

    public class ReservationMonthRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }
}
