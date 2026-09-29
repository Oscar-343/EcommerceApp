namespace EcommerceApp.Models.Reports
{
    // "Plantilla" de datos de un reporte exportable. Cada reporte del admin arma UNO de estos
    // y lo mismo se dibuja en Excel (ReporteExcel) y en PDF (ReportePdf), con el mismo diseño.
    public class ReporteDocumento
    {
        public string Titulo { get; set; } = string.Empty;       // "Reporte de ingresos"
        public string Descripcion { get; set; } = string.Empty;  // una línea que explica qué cuenta
        public string Periodo { get; set; } = string.Empty;      // "01/09/2026 al 30/09/2026"

        // Tarjetas de resumen (arriba del reporte): etiqueta + valor ya formateado.
        public List<ReporteDato> Resumen { get; set; } = new();

        // Tablas del reporte, en orden.
        public List<ReporteTabla> Tablas { get; set; } = new();
    }

    public record ReporteDato(string Etiqueta, string Valor);

    public class ReporteTabla
    {
        public string Titulo { get; set; } = string.Empty;
        public string[] Columnas { get; set; } = Array.Empty<string>();

        // Cada celda puede ser string, int o decimal. Los números se alinean a la derecha
        // y en Excel quedan como número (se pueden sumar).
        public List<object?[]> Filas { get; set; } = new();

        // Índices de columnas que son dinero (se muestran como "Bs. 1,234.00").
        public HashSet<int> ColumnasMoneda { get; set; } = new();

        // Si es true, la última fila se pinta como fila de total.
        public bool UltimaFilaEsTotal { get; set; }
    }
}
