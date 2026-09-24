using System.Globalization;
using System.Text.RegularExpressions;

namespace EcommerceApp.Services
{
    /// <summary>
    /// Obtiene latitud y longitud a partir de un enlace de Google Maps
    /// (largo o corto de "Compartir") o de coordenadas pegadas directamente.
    /// </summary>
    public class MapLinkService(HttpClient http)
    {
        // Un número de coordenada: signo opcional, hasta 3 dígitos y decimales.
        // Se usa [0-9] y no \d porque en .NET \d también acepta dígitos de otros alfabetos.
        private const string Num = @"(-?[0-9]{1,3}(?:\.[0-9]+)?)";

        // En orden de prioridad: del dato más preciso al menos preciso.
        private static readonly Regex[] Patrones =
        {
            new($@"!3d{Num}!4d{Num}"),                                               // punto exacto del lugar
            new($@"[?&](?:q|query|ll|destination|daddr)={Num}\s*,[\s+]*{Num}"),      // ?q=lat,lng
            new($@"@{Num},{Num}"),                                                   // centro de la pantalla
            new($@"^\s*{Num}\s*,\s*{Num}\s*$")                                       // "-17.35, -65.86"
        };

        // Dominios de Google permitidos (google.com, google.com.bo, maps.google.com...).
        private static readonly Regex DominioGoogle =
            new(@"^([a-z0-9-]+\.)*google\.(com|com\.[a-z]{2}|[a-z]{2})$", RegexOptions.IgnoreCase);

        public static (double Lat, double Lng)? ExtraerCoordenadas(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return null;

            // Los enlaces traen caracteres codificados (ej. %2C en lugar de la coma).
            var limpio = Uri.UnescapeDataString(texto);

            foreach (var patron in Patrones)
            {
                var m = patron.Match(limpio);
                if (!m.Success) continue;

                var lat = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                var lng = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);

                if (lat is >= -90 and <= 90 && lng is >= -180 and <= 180)
                    return (lat, lng);
            }

            return null;
        }

        public async Task<(double Lat, double Lng)?> ObtenerCoordenadasAsync(string? entrada)
        {
            if (string.IsNullOrWhiteSpace(entrada))
                return null;

            entrada = entrada.Trim();

            // 1. ¿Ya trae coordenadas? (enlace largo o coordenadas pegadas)
            var directas = ExtraerCoordenadas(entrada);
            if (directas != null)
                return directas;

            // 2. Enlace corto: seguir las redirecciones una por una (máximo 5).
            if (!Uri.TryCreate(entrada, UriKind.Absolute, out var url) || !EsDominioPermitido(url))
                return null;

            try
            {
                for (var i = 0; i < 5; i++)
                {
                    using var respuesta = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                    var destino = respuesta.Headers.Location;
                    if (destino == null)
                        return null; // ya no redirige y no se encontraron coordenadas

                    if (!destino.IsAbsoluteUri)
                        destino = new Uri(url, destino);

                    var coordenadas = ExtraerCoordenadas(destino.ToString());
                    if (coordenadas != null)
                        return coordenadas;

                    // Nunca se visita un dominio que no sea de Google.
                    if (!EsDominioPermitido(destino))
                        return null;

                    url = destino;
                }
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { } // tiempo de espera agotado

            return null;
        }

        private static bool EsDominioPermitido(Uri url)
        {
            if (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp)
                return false;

            var host = url.Host.ToLowerInvariant();
            return host == "maps.app.goo.gl" || host == "goo.gl" || DominioGoogle.IsMatch(host);
        }
    }
}
