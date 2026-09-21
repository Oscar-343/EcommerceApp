using System.Globalization;

namespace EcommerceApp.Helpers
{
    // Formato único de precio para todo el sitio: "Bs. " + número, sin depender de la cultura del servidor
    // (ToString("C") variaba entre "$" y "¤" según cómo esté configurado el servidor en Render).
    public static class PriceFormatter
    {
        public static string Format(decimal amount) => "Bs. " + amount.ToString("N2", CultureInfo.InvariantCulture);

        public static string Format(decimal? amount) => amount.HasValue ? Format(amount.Value) : "—";
    }
}
