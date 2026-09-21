namespace EcommerceApp.Models.Reports
{
    // Reporte de ingresos: solo cuenta reservas Acabado y pedidos Entregado.
    public class IncomeReportViewModel
    {
        public DateOnly Desde { get; set; }
        public DateOnly Hasta { get; set; }

        public decimal TotalReservas { get; set; }
        public decimal TotalProductos { get; set; }
        public decimal TotalGeneral => TotalReservas + TotalProductos;

        public List<IncomeMonthRow> PorMes { get; set; } = new();

        // Solicitudes abiertas que todavía no cuentan como ingreso (excluidas del total de arriba).
        public decimal PorConfirmarReservas { get; set; }
        public decimal PorConfirmarPedidos { get; set; }
        public decimal PorConfirmarTotal => PorConfirmarReservas + PorConfirmarPedidos;
    }

    public class IncomeMonthRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Reservas { get; set; }
        public decimal Productos { get; set; }
        public decimal Total => Reservas + Productos;
    }
}
