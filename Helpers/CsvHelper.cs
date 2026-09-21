using System.Text;

namespace EcommerceApp.Helpers
{
    // Construye archivos CSV simples para los reportes: separador ';', UTF-8 con BOM
    // (para que Excel en español lo abra bien) y protección contra inyección de fórmulas.
    public static class CsvHelper
    {
        // Si la celda empieza con = + - @ antepone ' (evita que Excel la interprete como fórmula).
        // Si tiene ; " o salto de línea, la envuelve en comillas dobles.
        public static string EscapeCell(string? value)
        {
            var text = value ?? string.Empty;
            if (text.Length > 0 && "=+-@".Contains(text[0]))
            {
                text = "'" + text;
            }
            if (text.Contains(';') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
            {
                text = "\"" + text.Replace("\"", "\"\"") + "\"";
            }
            return text;
        }

        // Une filas (cada una ya como arreglo de celdas sin escapar) en un solo texto CSV.
        public static string BuildCsv(IEnumerable<string[]> rows)
        {
            var sb = new StringBuilder();
            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(';', row.Select(EscapeCell)));
            }
            return sb.ToString();
        }

        // Bytes UTF-8 con BOM, listos para devolver con File(...).
        public static byte[] ToUtf8Bytes(string csv) => new UTF8Encoding(true).GetBytes(csv);
    }
}
