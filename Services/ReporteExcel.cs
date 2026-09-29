using ClosedXML.Excel;
using EcommerceApp.Models.Reports;

namespace EcommerceApp.Services
{
    // Dibuja un ReporteDocumento como archivo .xlsx con el diseño de Tren al Sur:
    // logo, título, franja verde, resumen, tablas con encabezado verde y filas alternadas.
    public static class ReporteExcel
    {
        private static readonly XLColor Verde = XLColor.FromHtml("#315D45");
        private static readonly XLColor VerdeClaro = XLColor.FromHtml("#557F63");
        private static readonly XLColor Oscuro = XLColor.FromHtml("#0B100E");
        private static readonly XLColor Gris = XLColor.FromHtml("#6B756E");
        private static readonly XLColor FilaAlterna = XLColor.FromHtml("#EEF3EF");
        private static readonly XLColor Borde = XLColor.FromHtml("#D5DDD7");
        private static readonly XLColor FondoResumen = XLColor.FromHtml("#F5F7F5");

        private const string FormatoMoneda = "\"Bs.\" #,##0.00";

        public static byte[] Generar(ReporteDocumento doc, byte[]? logo)
        {
            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Reporte");
            hoja.ShowGridLines = false;
            hoja.Style.Font.FontName = "Calibri";
            hoja.Style.Font.FontSize = 10;

            // Ancho del reporte = la tabla con más columnas (mínimo 4 para que quepa el encabezado).
            var columnas = Math.Max(4, doc.Tablas.Select(t => t.Columnas.Length).DefaultIfEmpty(0).Max());

            // ---------- Encabezado: logo + marca + título + periodo ----------
            hoja.Column(1).Width = 11;
            hoja.Row(1).Height = 18;
            hoja.Row(2).Height = 26;
            hoja.Row(3).Height = 16;
            hoja.Row(4).Height = 16;

            if (logo != null)
            {
                using var stream = new MemoryStream(logo);
                hoja.AddPicture(stream).MoveTo(hoja.Cell(1, 1)).WithSize(62, 62);
            }

            Texto(hoja.Cell(1, 2), "TREN AL SUR", 9, true, VerdeClaro);
            Texto(hoja.Cell(2, 2), doc.Titulo.ToUpperInvariant(), 18, true, Oscuro);
            Texto(hoja.Cell(3, 2), doc.Descripcion, 10, false, Gris);
            Texto(hoja.Cell(4, 2), $"Periodo: {doc.Periodo}   ·   Generado: {HoraBolivia():dd/MM/yyyy HH:mm}", 9, false, Gris);

            // Franja verde de separación
            var franja = hoja.Range(5, 1, 5, columnas);
            franja.Style.Fill.BackgroundColor = Verde;
            hoja.Row(5).Height = 5;

            var fila = 7;

            // ---------- Resumen (tarjetas) ----------
            if (doc.Resumen.Count > 0)
            {
                Seccion(hoja, fila++, "Resumen", columnas);
                foreach (var dato in doc.Resumen)
                {
                    var etiqueta = hoja.Cell(fila, 1);
                    etiqueta.Value = dato.Etiqueta;
                    hoja.Range(fila, 1, fila, 2).Merge();
                    var valor = hoja.Cell(fila, 3);
                    valor.Value = dato.Valor;
                    valor.Style.Font.Bold = true;
                    valor.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    var rango = hoja.Range(fila, 1, fila, 3);
                    rango.Style.Fill.BackgroundColor = FondoResumen;
                    rango.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    rango.Style.Border.BottomBorderColor = Borde;
                    hoja.Row(fila).Height = 18;
                    fila++;
                }
                fila++;
            }

            // ---------- Tablas ----------
            foreach (var tabla in doc.Tablas)
            {
                Seccion(hoja, fila++, tabla.Titulo, columnas);

                // Encabezado de columnas
                for (var c = 0; c < tabla.Columnas.Length; c++)
                {
                    var celda = hoja.Cell(fila, c + 1);
                    celda.Value = tabla.Columnas[c];
                    celda.Style.Font.Bold = true;
                    celda.Style.Font.FontColor = XLColor.White;
                    celda.Style.Fill.BackgroundColor = Verde;
                    celda.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    celda.Style.Alignment.Horizontal = EsColumnaNumerica(tabla, c)
                        ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Left;
                }
                hoja.Row(fila).Height = 20;
                var filaEncabezado = fila;
                fila++;

                if (tabla.Filas.Count == 0)
                {
                    var vacia = hoja.Cell(fila, 1);
                    vacia.Value = "Sin datos en este periodo.";
                    vacia.Style.Font.Italic = true;
                    vacia.Style.Font.FontColor = Gris;
                    fila += 2;
                    continue;
                }

                for (var i = 0; i < tabla.Filas.Count; i++)
                {
                    var datos = tabla.Filas[i];
                    var esTotal = tabla.UltimaFilaEsTotal && i == tabla.Filas.Count - 1;

                    for (var c = 0; c < tabla.Columnas.Length; c++)
                    {
                        var celda = hoja.Cell(fila, c + 1);
                        var valor = c < datos.Length ? datos[c] : null;
                        switch (valor)
                        {
                            case decimal d:
                                celda.Value = (double)d;
                                celda.Style.NumberFormat.Format = tabla.ColumnasMoneda.Contains(c) ? FormatoMoneda : "#,##0.##";
                                celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                                break;
                            case int n:
                                celda.Value = n;
                                celda.Style.NumberFormat.Format = "#,##0";
                                celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                                break;
                            default:
                                celda.Value = valor?.ToString() ?? "";
                                break;
                        }
                    }

                    var rango = hoja.Range(fila, 1, fila, tabla.Columnas.Length);
                    rango.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    rango.Style.Border.BottomBorderColor = Borde;
                    if (esTotal)
                    {
                        rango.Style.Font.Bold = true;
                        rango.Style.Border.TopBorder = XLBorderStyleValues.Medium;
                        rango.Style.Border.TopBorderColor = Verde;
                    }
                    else if (i % 2 == 1)
                    {
                        rango.Style.Fill.BackgroundColor = FilaAlterna;
                    }
                    fila++;
                }

                // Filtros en el encabezado de cada tabla (flechitas para ordenar/filtrar en Excel).
                if (doc.Tablas.Count == 1)
                {
                    var ultimaFila = tabla.UltimaFilaEsTotal ? fila - 2 : fila - 1;   // el total queda fuera del filtro
                    if (ultimaFila > filaEncabezado)
                        hoja.Range(filaEncabezado, 1, ultimaFila, tabla.Columnas.Length).SetAutoFilter();
                }

                fila++;
            }

            // ---------- Anchos de columna (calculados a mano: no depende de fuentes del servidor) ----------
            for (var c = 0; c < columnas; c++)
            {
                var maximo = doc.Tablas
                    .SelectMany(t => t.Filas.Select(f => c < f.Length ? Largo(f[c], t.ColumnasMoneda.Contains(c)) : 0)
                        .Append(c < t.Columnas.Length ? t.Columnas[c].Length : 0))
                    .DefaultIfEmpty(0).Max();
                if (c < 3) maximo = Math.Max(maximo, doc.Resumen.Select(r => c == 2 ? r.Valor.Length : r.Etiqueta.Length / 2).DefaultIfEmpty(0).Max());
                hoja.Column(c + 1).Width = Math.Clamp(maximo + 4, 12, 50);
            }

            // ---------- Impresión: horizontal, a lo ancho de la hoja, con pie de página ----------
            hoja.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            hoja.PageSetup.FitToPages(1, 0);
            hoja.PageSetup.Margins.Top = 0.5;
            hoja.PageSetup.Margins.Bottom = 0.6;
            hoja.PageSetup.Footer.Left.AddText("Tren al Sur · " + doc.Titulo);
            hoja.PageSetup.Footer.Right.AddText(XLHFPredefinedText.PageNumber);

            using var salida = new MemoryStream();
            libro.SaveAs(salida);
            return salida.ToArray();
        }

        private static void Texto(IXLCell celda, string texto, double tamano, bool negrita, XLColor color)
        {
            celda.Value = texto;
            celda.Style.Font.FontSize = tamano;
            celda.Style.Font.Bold = negrita;
            celda.Style.Font.FontColor = color;
        }

        // Título de sección: texto verde en mayúsculas con línea inferior.
        private static void Seccion(IXLWorksheet hoja, int fila, string titulo, int columnas)
        {
            var celda = hoja.Cell(fila, 1);
            celda.Value = titulo.ToUpperInvariant();
            celda.Style.Font.Bold = true;
            celda.Style.Font.FontSize = 11;
            celda.Style.Font.FontColor = Verde;
            var rango = hoja.Range(fila, 1, fila, columnas);
            rango.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            rango.Style.Border.BottomBorderColor = VerdeClaro;
            hoja.Row(fila).Height = 22;
        }

        private static bool EsColumnaNumerica(ReporteTabla tabla, int c) =>
            tabla.Filas.Any(f => c < f.Length && (f[c] is decimal || f[c] is int));

        private static int Largo(object? valor, bool moneda) => valor switch
        {
            decimal d => d.ToString("#,##0.00").Length + (moneda ? 4 : 0),
            int n => n.ToString("#,##0").Length,
            _ => valor?.ToString()?.Length ?? 0
        };

        private static DateTime HoraBolivia() => DateTime.UtcNow.AddHours(-4);
    }
}
