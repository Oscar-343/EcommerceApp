using System.Globalization;
using EcommerceApp.Models.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EcommerceApp.Services
{
    // Dibuja un ReporteDocumento como PDF con el diseño de Tren al Sur (mismo contenido que el Excel):
    // encabezado con logo, tarjetas de resumen, tablas con encabezado verde y pie con número de página.
    public static class ReportePdf
    {
        private const string Verde = "#315D45";
        private const string VerdeClaro = "#557F63";
        private const string Oscuro = "#0B100E";
        private const string Gris = "#6B756E";
        private const string FilaAlterna = "#EEF3EF";
        private const string Borde = "#D5DDD7";
        private const string FondoTarjeta = "#F5F7F5";
        private const string Blanco = "#FFFFFF";

        public static byte[] Generar(ReporteDocumento doc, byte[]? logo)
        {
            return Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4.Landscape());
                    pagina.Margin(28);
                    pagina.PageColor(Colors.White);
                    pagina.DefaultTextStyle(x => x.FontSize(9).FontColor(Oscuro));

                    pagina.Header().Element(c => Encabezado(c, doc, logo));
                    pagina.Content().PaddingVertical(14).Element(c => Contenido(c, doc));
                    pagina.Footer().Element(c => Pie(c, doc));
                });
            }).GeneratePdf();
        }

        private static void Encabezado(IContainer contenedor, ReporteDocumento doc, byte[]? logo)
        {
            contenedor.Column(col =>
            {
                col.Item().Row(fila =>
                {
                    if (logo != null)
                        fila.ConstantItem(56).Height(56).Image(logo).FitArea();

                    fila.RelativeItem().PaddingLeft(logo != null ? 12 : 0).Column(texto =>
                    {
                        texto.Item().Text("TREN AL SUR").FontSize(8).Bold().FontColor(VerdeClaro).LetterSpacing(0.15f);
                        texto.Item().Text(doc.Titulo.ToUpperInvariant()).FontSize(18).Bold().FontColor(Oscuro);
                        texto.Item().Text(doc.Descripcion).FontSize(9).FontColor(Gris);
                    });

                    fila.ConstantItem(170).AlignRight().AlignBottom().Column(datos =>
                    {
                        datos.Item().AlignRight().Text($"Periodo: {doc.Periodo}").FontSize(8.5f).SemiBold();
                        datos.Item().AlignRight().Text($"Generado: {DateTime.UtcNow.AddHours(-4):dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Gris);
                    });
                });

                // Franja verde bajo el encabezado
                col.Item().PaddingTop(10).Height(4).Background(Verde);
            });
        }

        private static void Contenido(IContainer contenedor, ReporteDocumento doc)
        {
            contenedor.Column(col =>
            {
                col.Spacing(16);

                // Tarjetas de resumen: 3 por fila
                if (doc.Resumen.Count > 0)
                {
                    col.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                        foreach (var dato in doc.Resumen)
                        {
                            tabla.Cell().Padding(4).Background(FondoTarjeta).BorderLeft(3).BorderColor(Verde)
                                .PaddingVertical(8).PaddingHorizontal(10).Column(tarjeta =>
                                {
                                    tarjeta.Item().Text(dato.Etiqueta).FontSize(8).FontColor(Gris);
                                    tarjeta.Item().Text(dato.Valor).FontSize(13).Bold().FontColor(Oscuro);
                                });
                        }
                    });
                }

                foreach (var t in doc.Tablas)
                    col.Item().Element(c => Tabla(c, t));
            });
        }

        private static void Tabla(IContainer contenedor, ReporteTabla t)
        {
            contenedor.Column(col =>
            {
                col.Item().PaddingBottom(6).BorderBottom(1.5f).BorderColor(VerdeClaro)
                    .Text(t.Titulo.ToUpperInvariant()).FontSize(10).Bold().FontColor(Verde);

                if (t.Filas.Count == 0)
                {
                    col.Item().PaddingTop(6).Text("Sin datos en este periodo.").Italic().FontColor(Gris);
                    return;
                }

                col.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        // Columnas de texto (nombres) más anchas; las numéricas, angostas.
                        for (var i = 0; i < t.Columnas.Length; i++)
                            c.RelativeColumn(EsNumerica(t, i) ? 1 : 2.4f);
                    });

                    tabla.Header(h =>
                    {
                        for (var i = 0; i < t.Columnas.Length; i++)
                        {
                            var celda = h.Cell().Background(Verde).PaddingVertical(6).PaddingHorizontal(6);
                            (EsNumerica(t, i) ? celda.AlignRight() : celda)
                                .Text(t.Columnas[i]).FontColor(Colors.White).Bold().FontSize(8.5f);
                        }
                    });

                    for (var f = 0; f < t.Filas.Count; f++)
                    {
                        var esTotal = t.UltimaFilaEsTotal && f == t.Filas.Count - 1;
                        var fondo = esTotal ? FondoTarjeta : (f % 2 == 1 ? FilaAlterna : Blanco);

                        for (var i = 0; i < t.Columnas.Length; i++)
                        {
                            var valor = i < t.Filas[f].Length ? t.Filas[f][i] : null;
                            // Fila de total: línea verde arriba. Resto: línea gris fina abajo.
                            var conBorde = esTotal
                                ? tabla.Cell().Background(fondo).BorderTop(1.5f).BorderColor(Verde)
                                : tabla.Cell().Background(fondo).BorderBottom(0.5f).BorderColor(Borde);
                            var celda = conBorde.PaddingVertical(5).PaddingHorizontal(6);

                            var texto = (EsNumerica(t, i) ? celda.AlignRight() : celda).Text(Formato(valor, t.ColumnasMoneda.Contains(i)));
                            if (esTotal) texto.Bold();
                        }
                    }
                });
            });
        }

        private static void Pie(IContainer contenedor, ReporteDocumento doc)
        {
            contenedor.BorderTop(0.5f).BorderColor(Borde).PaddingTop(6).Row(fila =>
            {
                fila.RelativeItem().Text($"Tren al Sur · {doc.Titulo} · Documento generado desde el panel de administración")
                    .FontSize(7.5f).FontColor(Gris);
                fila.ConstantItem(90).AlignRight().Text(x =>
                {
                    x.DefaultTextStyle(s => s.FontSize(7.5f).FontColor(Gris));
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        }

        private static bool EsNumerica(ReporteTabla t, int i) =>
            t.Filas.Any(f => i < f.Length && (f[i] is decimal || f[i] is int));

        private static string Formato(object? valor, bool moneda) => valor switch
        {
            decimal d when moneda => "Bs. " + d.ToString("N2", CultureInfo.InvariantCulture),
            decimal d => d.ToString("#,##0.##", CultureInfo.InvariantCulture),
            int n => n.ToString("N0", CultureInfo.InvariantCulture),
            _ => valor?.ToString() ?? ""
        };
    }
}
