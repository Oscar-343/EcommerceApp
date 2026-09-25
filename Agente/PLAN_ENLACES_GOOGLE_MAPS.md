# Plan: Pegar enlaces de Google Maps en el formulario de rutas — Tren al Sur

> **Para Claude Code.** Lee este archivo completo antes de empezar.
> El proyecto es universitario y el dueño lo va a **estudiar después**. Manda la simplicidad sobre la elegancia.

## Objetivo

En el formulario de crear y editar ruta del admin, el administrador **pega un enlace de Google Maps** del punto de inicio y del punto final, y el sistema **llena solos** los campos de latitud y longitud.

## Decisiones ya tomadas (no las cambies sin preguntarme)

1. **En la base se siguen guardando latitud y longitud.** El enlace es solo una forma cómoda de llenarlas: **no se guarda**. No hay migración ni cambios en el modelo `Service`.
2. **Los mapas públicos no se tocan** (`rutas-map.js`, Home, `Services/Index`, `Services/Details`).
3. **El enlace lo lee el servidor**, no el navegador. Los enlaces cortos del botón "Compartir" (`maps.app.goo.gl/...`) redirigen al enlace largo, y el navegador no puede seguir esa redirección.
4. **Solo se siguen enlaces de dominios de Google.** El servidor nunca visita otras direcciones, aunque el administrador pegue cualquier cosa.
5. **Los campos de latitud y longitud siguen visibles y editables**, como respaldo si un enlace no trae coordenadas.
6. También se aceptan **coordenadas pegadas directamente** (`-17.35, -65.8667`), que es lo que copia Google Maps con clic derecho sobre un punto.

## Reglas

1. **Código simple.** Sin paquetes NuGet ni librerías JS nuevas. Comentarios breves en español solo donde expliquen el *por qué*.
2. Respeta `Agente/AGENTS.md` y `Agente/.agent/*.md`. Si este plan los contradice, avísame antes de actuar.
3. **No inventes funcionalidades** fuera de este plan (por ejemplo, un botón "Cómo llegar" en la web pública). Si algo requiere una decisión, pregúntame.
4. No toques migraciones ni `appsettings*.json`. Nunca imprimas secretos.
5. Rama `enlaces-maps`. **Un commit por fase. No hagas push.**
6. Después de cada fase: `dotnet build` sin errores y dime qué probar a mano.

---

## Fase 1 — Servicio que lee el enlace

### Archivo nuevo: `Services/MapLinkService.cs`

Tiene dos partes:

- **`ExtraerCoordenadas(texto)`**: busca coordenadas dentro de un texto. Es estática y no usa internet.
- **`ObtenerCoordenadasAsync(enlace)`**: si el texto no trae coordenadas, sigue las redirecciones del enlace (solo dominios de Google) y vuelve a buscar.

Orden de búsqueda (importa):

1. `!3d<lat>!4d<lng>` → el **punto exacto** del lugar (está en los enlaces largos de `/place/`).
2. `q=`, `query=`, `ll=`, `destination=` o `daddr=` seguidos de `lat,lng`.
3. `@<lat>,<lng>` → es el **centro de la pantalla**, no siempre el punto marcado. Por eso va después.
4. Texto que solo contiene `lat, lng`.

```csharp
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
```

### `Program.cs`

Junto a las otras líneas `builder.Services...`:

```csharp
// Lee enlaces de Google Maps en el admin. AllowAutoRedirect = false:
// cada redirección se revisa a mano para no salir de los dominios de Google.
builder.Services.AddHttpClient<MapLinkService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; TrenAlSur/1.0)");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
```

---

## Fase 2 — Acción del admin

### `Controllers/AdminController.Services.cs`

Agrega esta acción. El servicio se recibe con `[FromServices]` para **no cambiar el constructor** de `AdminController`, que comparten todas las clases parciales. La clase ya tiene `[Authorize(Roles = "Admin")]`, así que solo la usan administradores.

```csharp
// Recibe un enlace de Google Maps (o coordenadas) y devuelve latitud y longitud.
// La usa el formulario de rutas al pegar el enlace; no guarda nada.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ResolveMapLink(string? url, [FromServices] MapLinkService mapLinks)
{
    var coordenadas = await mapLinks.ObtenerCoordenadasAsync(url);

    if (coordenadas == null)
        return Json(new { ok = false, mensaje = "No encontré coordenadas en este enlace. Marca el punto exacto en Google Maps y vuelve a compartirlo." });

    return Json(new { ok = true, lat = coordenadas.Value.Lat, lng = coordenadas.Value.Lng });
}
```

---

## Fase 3 — Formulario

### `Views/Admin/ServiceCreate.cshtml` y `Views/Admin/ServiceEdit.cshtml`

Reemplaza la sección **"Coordenadas del mapa"** (la de la Fase 2 del plan anterior) por esta. Los campos del enlace **no tienen `name`** a propósito: así no se envían al guardar y no llegan al modelo.

- Se usa `type="text"` y no `type="url"`, para que también se puedan pegar coordenadas sueltas sin que el navegador bloquee el envío.
- Los `id` `StartLatitude`, `StartLongitude`, `EndLatitude` y `EndLongitude` los genera `asp-for`: el script los usa para llenarlos.

```html
<div class="admin-form-section">
    <h3>Ubicación en el mapa</h3>
    <p class="hint">
        En Google Maps haz clic en el punto exacto (aparece un pin), luego <strong>Compartir → Copiar enlace</strong> y pégalo aquí.
        También puedes pegar coordenadas copiadas con clic derecho.
    </p>

    <div class="form-grid">
        <div class="form-field">
            <label for="StartMapLink">Enlace de Google Maps — punto de inicio</label>
            <input id="StartMapLink" type="text" inputmode="url" autocomplete="off" class="form-control"
                   placeholder="https://maps.app.goo.gl/..."
                   data-map-link
                   data-resolve-url="@Url.Action("ResolveMapLink", "Admin")"
                   data-lat-target="StartLatitude" data-lng-target="StartLongitude"
                   data-status-id="StartMapLinkStatus" />
            <small id="StartMapLinkStatus" class="map-link-status" aria-live="polite"></small>
        </div>
        <div class="form-field">
            <label for="EndMapLink">Enlace de Google Maps — punto final</label>
            <input id="EndMapLink" type="text" inputmode="url" autocomplete="off" class="form-control"
                   placeholder="Opcional si la ruta es un circuito"
                   data-map-link
                   data-resolve-url="@Url.Action("ResolveMapLink", "Admin")"
                   data-lat-target="EndLatitude" data-lng-target="EndLongitude"
                   data-status-id="EndMapLinkStatus" />
            <small id="EndMapLinkStatus" class="map-link-status" aria-live="polite"></small>
        </div>
    </div>

    <p class="hint">Coordenadas (se llenan solas al pegar el enlace; puedes corregirlas a mano).</p>
    <div class="form-grid">
        <div class="form-field">
            <label asp-for="StartLatitude">Latitud de inicio</label>
            <input asp-for="StartLatitude" type="number" step="any" class="form-control" />
            <span asp-validation-for="StartLatitude" class="text-danger"></span>
        </div>
        <div class="form-field">
            <label asp-for="StartLongitude">Longitud de inicio</label>
            <input asp-for="StartLongitude" type="number" step="any" class="form-control" />
            <span asp-validation-for="StartLongitude" class="text-danger"></span>
        </div>
        <div class="form-field">
            <label asp-for="EndLatitude">Latitud final</label>
            <input asp-for="EndLatitude" type="number" step="any" class="form-control" />
            <span asp-validation-for="EndLatitude" class="text-danger"></span>
        </div>
        <div class="form-field">
            <label asp-for="EndLongitude">Longitud final</label>
            <input asp-for="EndLongitude" type="number" step="any" class="form-control" />
            <span asp-validation-for="EndLongitude" class="text-danger"></span>
        </div>
    </div>
</div>
```

En el `@section Scripts` de ambas vistas, **antes** del `<script>` que ya existe:

```html
<script src="~/js/admin-map-link.js" asp-append-version="true"></script>
```

### Archivo nuevo: `wwwroot/js/admin-map-link.js`

```javascript
/**
 * Formulario de rutas (admin): al pegar un enlace de Google Maps,
 * pide las coordenadas al servidor (Admin/ResolveMapLink) y llena
 * los campos de latitud y longitud.
 */
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-map-link]').forEach(iniciarCampo);
    });

    function iniciarCampo(input) {
        var estado = document.getElementById(input.dataset.statusId);
        var campoLat = document.getElementById(input.dataset.latTarget);
        var campoLng = document.getElementById(input.dataset.lngTarget);
        var urlServidor = input.dataset.resolveUrl;
        var ultimoEnlace = '';

        // En "Editar": si la ruta ya tiene coordenadas, se muestran para verificarlas.
        if (campoLat.value && campoLng.value) {
            mostrarUbicacion(estado, 'Ubicación actual:', campoLat.value, campoLng.value);
        }

        function resolver() {
            var enlace = input.value.trim();
            if (!enlace || enlace === ultimoEnlace) return; // evita consultar dos veces lo mismo
            ultimoEnlace = enlace;
            mostrarMensaje(estado, 'Buscando coordenadas…', '');

            var datos = new FormData();
            datos.append('url', enlace);
            datos.append('__RequestVerificationToken', obtenerToken(input));

            fetch(urlServidor, { method: 'POST', body: datos })
                .then(function (respuesta) { return respuesta.json(); })
                .then(function (resultado) {
                    if (!resultado.ok) {
                        mostrarMensaje(estado, resultado.mensaje, 'error');
                        return;
                    }
                    campoLat.value = resultado.lat;
                    campoLng.value = resultado.lng;
                    mostrarUbicacion(estado, '✓', resultado.lat, resultado.lng);
                })
                .catch(function () {
                    ultimoEnlace = ''; // permite reintentar
                    mostrarMensaje(estado, 'No se pudo consultar el enlace. Ingresa las coordenadas a mano.', 'error');
                });
        }

        // "paste" ocurre antes de que el texto llegue al campo: se espera un instante.
        input.addEventListener('paste', function () { setTimeout(resolver, 0); });
        input.addEventListener('change', resolver);

        // Enter en este campo no debe enviar el formulario completo.
        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); resolver(); }
        });
    }

    // Texto simple de estado (textContent: nada se interpreta como HTML).
    function mostrarMensaje(estado, texto, tipo) {
        estado.className = 'map-link-status' + (tipo ? ' map-link-status--' + tipo : '');
        estado.textContent = texto;
    }

    // "✓ -17.35, -65.8667  Ver en el mapa" con enlace para comprobar el punto.
    function mostrarUbicacion(estado, prefijo, lat, lng) {
        mostrarMensaje(estado, prefijo + ' ' + lat + ', ' + lng, 'ok');
        var link = document.createElement('a');
        link.href = 'https://www.google.com/maps?q=' + encodeURIComponent(lat + ',' + lng);
        link.target = '_blank';
        link.rel = 'noopener';
        link.textContent = 'Ver en el mapa';
        estado.appendChild(link);
    }

    function obtenerToken(input) {
        var campo = input.form && input.form.querySelector('input[name="__RequestVerificationToken"]');
        return campo ? campo.value : '';
    }
})();
```

### `wwwroot/css/admin.css`

Al final:

```css
/* Estado del enlace de Google Maps (formulario de rutas) */
.map-link-status {
    display: block;
    min-height: 1.2em;
    margin-top: 0.35rem;
    font-size: 0.8rem;
    color: #A8B0AA;
}

.map-link-status--ok { color: #557F63; }
.map-link-status--error { color: #d9534f; }

.map-link-status a { margin-left: 0.5rem; }
```

---

## Fase 4 — Documentación

- Anota en `Agente/.agent/PROJECT_STATE.md`: el formulario de rutas acepta enlaces de Google Maps; se siguen guardando solo latitud y longitud.
- Anota en `Agente/.agent/TASKS.md` la limitación conocida (ver abajo).

---

## Pruebas a mano

Prueba en **Crear** y en **Editar**, con enlaces reales:

- [ ] Enlace corto del botón **Compartir** de un pin colocado a mano (`maps.app.goo.gl/...`) → llena las coordenadas.
- [ ] Enlace largo copiado de la barra del navegador (`google.com/maps/place/...`) → llena las coordenadas del punto exacto.
- [ ] Enlace con `?q=-17.35,-65.8667` → llena las coordenadas.
- [ ] Coordenadas pegadas con clic derecho (`-17.35, -65.8667`) → llena las coordenadas.
- [ ] Enlace compartido de un **negocio o lugar con nombre**: anota si trae coordenadas o muestra el aviso. No es un error si muestra el aviso.
- [ ] Una URL que no es de Google (ej. `https://example.com`) → aviso, sin llenar nada.
- [ ] Texto cualquiera → aviso.
- [ ] "Ver en el mapa" abre Google Maps en el punto correcto.
- [ ] Enter dentro del campo del enlace no envía el formulario.
- [ ] Guardar la ruta: las coordenadas quedan en la base y el mapa del detalle muestra inicio y fin.
- [ ] En **Editar**, al abrir una ruta con coordenadas, aparece "Ubicación actual" con su enlace.
- [ ] Repite la prueba del enlace corto **desplegado en Render**: allí el servidor también debe poder seguir la redirección.

## Limitación conocida (avisarme, no es un bug a resolver)

Al compartir un **negocio o lugar con nombre**, Google a veces redirige a una búsqueda por nombre **sin coordenadas**. En ese caso se muestra el aviso y el administrador debe marcar el punto exacto (pin) y volver a compartirlo, o pegar las coordenadas con clic derecho. Si Google cambia el formato de sus enlaces, los campos de latitud y longitud siguen funcionando como respaldo.

## Fuera de alcance (no lo hagas)

- Guardar el enlace en la base de datos.
- Botón "Cómo llegar" o enlaces a Google Maps en la web pública.
- API de Google Maps o cualquier servicio con API key.
- Cambios en `rutas-map.js` o en los mapas públicos.
