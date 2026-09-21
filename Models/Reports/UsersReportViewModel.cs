namespace EcommerceApp.Models.Reports
{
    // Reporte de usuarios: registros por mes.
    public class UsersReportViewModel
    {
        public DateOnly Desde { get; set; }
        public DateOnly Hasta { get; set; }

        public int TotalRegistrados { get; set; }
        public List<UserMonthRow> PorMes { get; set; } = new();
    }

    public class UserMonthRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Cantidad { get; set; }
    }
}
