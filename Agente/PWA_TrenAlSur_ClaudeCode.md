# Implementación PWA — Tren al Sur

> Instrucciones para Claude Code. Léelas completas antes de modificar cualquier archivo.

## 0. Contexto y reglas

**Tren al Sur** es un e-commerce outdoor (senderismo, trekking, camping) con una sección de Rutas/Servicios.

- **Stack:** ASP.NET Core MVC + ASP.NET Core Web API (proyectos separados), EF Core, PostgreSQL (Supabase), Supabase Storage para imágenes y videos, Bootstrap, JavaScript/jQuery.
- **Objetivo de esta tarea:** convertir la aplicación MVC en una PWA instalable, con caché segura y soporte offline básico. Además, agregar un botón **discreto** de "Instalar app" con estilo outdoor.

**Reglas obligatorias:**

1. **Explora primero, luego implementa.** Antes de crear archivos, inspecciona la solución:
   - la ubicación de `wwwroot`;
   - el `_Layout.cshtml` principal;
   - `Program.cs` (static files, HTTPS, autenticación);
   - los nombres reales de los controladores y las rutas: Home, Productos, Rutas/Servicios, Carrito, Account/Login, Admin.

   No asumas rutas ni nombres. Usa los que existan.
2. **No cambies la arquitectura.** No toques Models, Services, Data, DTOs ni el proyecto API salvo que sea estrictamente necesario, y en ese caso justifícalo.
3. **No inventes funcionalidades.** Nada de notificaciones push, background sync, sistemas de favoritos offline ni nuevas entidades. Solo lo que se pide aquí.
4. **Código limpio y simple.** Usa JavaScript vanilla para el Service Worker, sin Workbox ni dependencias nuevas, y comenta las decisiones importantes.
5. Si algo del proyecto contradice estas instrucciones, **detente y reporta** en lugar de forzar cambios.

---

## 1. Alcance

| Incluido | Excluido (NO implementar) |
|---|---|
| Web App Manifest | Notificaciones push |
| Íconos PWA | Background Sync |
| Service Worker con estrategias de caché | Caché de carrito, checkout o auth |
| Página offline con estilo de la marca | Cambios en la API |
| Rutas visitadas disponibles sin conexión | Nuevas tablas o entidades |
| Botón discreto "Instalar app" | Librerías externas nuevas |

---

## 2. Archivos a crear o modificar

```
EcommerceApp (proyecto MVC)/
├── wwwroot/
│   ├── manifest.webmanifest      ← NUEVO
│   ├── service-worker.js         ← NUEVO (en la RAÍZ de wwwroot, no en /js)
│   ├── offline.html              ← NUEVO (HTML estático, no Razor)
│   ├── icons/                    ← NUEVO
│   │   ├── icon-192.png
│   │   ├── icon-512.png
│   │   ├── icon-maskable-512.png
│   │   └── apple-touch-icon.png  (180x180)
│   ├── css/pwa-install.css       ← NUEVO
│   └── js/pwa.js                 ← NUEVO (registro del SW + botón instalar)
├── Views/Shared/_Layout.cshtml   ← MODIFICAR
└── Views/Shared/_PwaInstallButton.cshtml ← NUEVO (partial)
```

> El Service Worker **debe** servirse desde la raíz (`/service-worker.js`) para que su scope cubra todo el sitio.

---

## 3. Manifest (`wwwroot/manifest.webmanifest`)

```json
{
  "name": "Tren al Sur — Equípate para explorar",
  "short_name": "Tren al Sur",
  "description": "Equipamiento outdoor, rutas y experiencias de aventura.",
  "lang": "es",
  "start_url": "/?source=pwa",
  "scope": "/",
  "display": "standalone",
  "orientation": "portrait-primary",
  "background_color": "#0B100E",
  "theme_color": "#0B100E",
  "icons": [
    { "src": "/icons/icon-192.png", "sizes": "192x192", "type": "image/png", "purpose": "any" },
    { "src": "/icons/icon-512.png", "sizes": "512x512", "type": "image/png", "purpose": "any" },
    { "src": "/icons/icon-maskable-512.png", "sizes": "512x512", "type": "image/png", "purpose": "maskable" }
  ]
}
```

**Tipo MIME:** ASP.NET Core puede no servir `.webmanifest` por defecto. En `Program.cs`, configura `FileExtensionContentTypeProvider` para mapear `.webmanifest` → `application/manifest+json` en `UseStaticFiles`. Hazlo sin romper la configuración existente de archivos estáticos.

**Íconos:** si el proyecto ya tiene un logo en `wwwroot`, genera los PNG a partir de él. Si no existe, crea placeholders simples: fondo `#0B100E`, silueta de montaña en `#557F63` y la iniciales "TS" en `#F1F3F1`. Informa al final que deben reemplazarse por el logo oficial. El ícono maskable debe dejar el contenido dentro del 80% central (zona segura).

---

## 4. Cambios en `_Layout.cshtml`

Dentro de `<head>`:

```html
<link rel="manifest" href="~/manifest.webmanifest" />
<meta name="theme-color" content="#0B100E" />
<meta name="mobile-web-app-capable" content="yes" />
<meta name="apple-mobile-web-app-capable" content="yes" />
<meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
<meta name="apple-mobile-web-app-title" content="Tren al Sur" />
<link rel="apple-touch-icon" href="~/icons/apple-touch-icon.png" />
<link rel="stylesheet" href="~/css/pwa-install.css" asp-append-version="true" />
```

Antes de cerrar `</body>`, **después** de los scripts existentes:

```html
<partial name="_PwaInstallButton" />
<script src="~/js/pwa.js" asp-append-version="true"></script>
```

**No incluyas** el botón ni el registro del SW en el layout del panel de administración, si existe uno separado.

---

## 5. Service Worker (`wwwroot/service-worker.js`)

### 5.1 Versionado

```js
const CACHE_VERSION = 'v1';
const STATIC_CACHE  = `tas-static-${CACHE_VERSION}`;
const PAGES_CACHE   = `tas-pages-${CACHE_VERSION}`;
const IMAGES_CACHE  = `tas-images-${CACHE_VERSION}`;
const OFFLINE_URL   = '/offline.html';
```

- En `install`: precachear `OFFLINE_URL`, los íconos y los CSS/JS principales (Bootstrap, jQuery, `site.css`, `site.js`). Usa las rutas reales encontradas en el layout. Después llama a `self.skipWaiting()`.
- En `activate`: borrar las cachés que empiecen con `tas-` y no coincidan con la versión actual. Después llama a `self.clients.claim()`.

### 5.2 Estrategias por tipo de petición

Implementar el `fetch` handler con este orden de evaluación. **La primera regla que coincide gana.**

| # | Condición | Estrategia |
|---|---|---|
| 1 | Método ≠ `GET` | **No interceptar** (`return;`) |
| 2 | Rutas sensibles: carrito, checkout, pago, account/login/logout/register, admin, `/api/` | **Network-only**, sin guardar nada |
| 3 | Peticiones con `credentials`/cookies de otros orígenes que no sean Supabase Storage | No interceptar |
| 4 | Estáticos propios (`.css`, `.js`, `.woff2`, `/icons/`, `/lib/`) | **Cache-first** → `STATIC_CACHE` |
| 5 | Imágenes de Supabase Storage (`*.supabase.co/storage/v1/object/public/*`) e imágenes locales | **Stale-while-revalidate** → `IMAGES_CACHE`, con límite de ~60 entradas |
| 6 | Navegación HTML (`request.mode === 'navigate'`) a Home, Productos, detalle de producto y Rutas/Servicios | **Network-first** → `PAGES_CACHE`. Si falla la red, se usa la caché y, si tampoco existe, `offline.html` |
| 7 | Cualquier otra navegación | Network, con fallback a `offline.html` |

**Reglas de seguridad del SW (obligatorias):**

- Guardar en caché solo respuestas con `response.ok === true` y `response.type` `basic` o `cors`. Las respuestas `opaque` solo se aceptan para imágenes de Supabase.
- **Nunca** guardar respuestas de páginas cuando el usuario está autenticado si la página muestra datos personales. Si el layout renderiza nombre de usuario o contador de carrito en el HTML, guarda la navegación en caché **solo** para visitantes anónimos. Detecta el caso según cómo esté implementada la autenticación (cookie o claim) y documenta la decisión en un comentario. Si no hay forma limpia de distinguirlo, guarda solo las páginas de **Rutas/Servicios** y reporta la limitación.
- Define la lista de prefijos sensibles como una constante clara al inicio del archivo, usando los nombres reales de los controladores.
- Implementa una función `trimCache(cacheName, maxEntries)` para limitar el tamaño de `IMAGES_CACHE` y `PAGES_CACHE` (~40 páginas).

### 5.3 Rutas offline

Las páginas de detalle de ruta que el usuario visitó quedan en `PAGES_CACHE` gracias a la estrategia network-first. **No se crea un sistema de "guardar ruta"**: basta con que una ruta visitada con conexión se pueda volver a abrir sin señal.

---

## 6. Página offline (`wwwroot/offline.html`)

Es un HTML autocontenido, con CSS inline y sin dependencias externas, porque debe funcionar sin red.

- **Fondo y colores:** fondo `#0B100E`, texto `#F1F3F1` y texto secundario `#A8B0AA`.
- **Ícono:** una ilustración SVG inline simple, por ejemplo la silueta de una montaña con un camino punteado en `#557F63`.
- **Título:** "Sin señal en el camino".
- **Texto:** "Parece que estás fuera de cobertura. Las rutas que ya visitaste siguen disponibles."
- **Botón "Reintentar":** hace `location.reload()`, con borde `#557F63` y hover de fondo `#315D45`.
- **Diseño:** centrado vertical, responsive y sin animaciones pesadas.

---

## 7. Botón "Instalar app"

### 7.1 Comportamiento (`wwwroot/js/pwa.js`)

1. **Registrar el SW** si `'serviceWorker' in navigator`, dentro de `window.addEventListener('load', …)`.
2. **Si la app ya está instalada**, no mostrar nada. Se considera instalada cuando `matchMedia('(display-mode: standalone)').matches` o `navigator.standalone === true`.
3. **En Chromium (Android, Chrome o Edge de escritorio):**
   - Escuchar `beforeinstallprompt`, llamar a `e.preventDefault()`, guardar el evento y **recién entonces** mostrar el botón.
   - Al hacer clic, llamar a `deferredPrompt.prompt()`, esperar `userChoice` y ocultar el botón.
   - Escuchar `appinstalled` para ocultar el botón definitivamente.
4. **En iOS Safari** (no existe `beforeinstallprompt`):
   - Detectar iOS + Safari y que no esté en modo standalone.
   - El botón abre un pequeño panel con instrucciones: "Toca **Compartir** y luego **Agregar a inicio**". Incluye un ícono SVG inline del botón compartir.
5. **Descarte:** el botón tiene una "×" pequeña. Al cerrarlo, guardar en `localStorage` (`tas-install-dismissed` con timestamp) y no volver a mostrarlo durante **14 días**. Envolver todo acceso a `localStorage` en `try/catch`.
6. **Momento de aparición:** mostrar el botón solo después de ~8 segundos en el sitio o tras el primer scroll significativo. No debe aparecer apenas carga la página.
7. **Rutas excluidas:** no mostrar el botón en checkout, login ni admin.
8. Sin jQuery obligatorio: usar JS vanilla para no depender del orden de carga.

### 7.2 Markup (`Views/Shared/_PwaInstallButton.cshtml`)

```html
<div id="tas-install" class="tas-install" hidden>
  <button type="button" class="tas-install__btn" id="tas-install-btn"
          aria-label="Instalar la app de Tren al Sur">
    <!-- SVG inline: montaña con flecha hacia abajo -->
    <span class="tas-install__label">Llévanos contigo</span>
  </button>
  <button type="button" class="tas-install__close" id="tas-install-close"
          aria-label="Ocultar">×</button>

  <div class="tas-install__ios" id="tas-install-ios" hidden role="dialog"
       aria-label="Cómo instalar en iPhone">
    <!-- Instrucciones iOS -->
  </div>
</div>
```

El texto principal es **"Llévanos contigo"**, con `title="Instalar app"`. En pantallas muy estrechas puede reducirse solo al ícono, manteniendo el `aria-label`.

### 7.3 Estilo (`wwwroot/css/pwa-install.css`)

**Principio:** debe ser discreto y parecer parte del equipo, no un banner publicitario.

- **Posición:** `fixed`, esquina inferior izquierda (`left: 16px; bottom: 16px`). La inferior derecha se reserva por si existen botones de carrito o WhatsApp; verifica que no choque con elementos fijos existentes.
- **Forma:** píldora compacta, con altura mínima de 44px (área táctil) y `border-radius: 999px`.
- **Colores:**
  - fondo: `rgba(23, 30, 26, 0.85)` (#171E1A translúcido) con `backdrop-filter: blur(8px)`;
  - borde: `1px solid #303832`;
  - texto: `#F1F3F1`, 0.85rem, `letter-spacing: 0.04em`, y ícono en `#557F63`.
- **Hover/focus:**
  - borde `#557F63`, elevación leve (`translateY(-2px)`) y sombra suave `0 8px 24px rgba(0,0,0,.35)`;
  - el ícono de la flecha baja 2px, como un gesto de "descarga";
  - `:focus-visible` con un outline claro en `#557F63`.
- **Entrada:** aparece con un fade + desplazamiento vertical de 12px en 400ms, con `ease-out`.
- **Movimiento reducido:** respetar `@media (prefers-reduced-motion: reduce)` y desactivar las transiciones.
- **Botón "×":** pequeño, color `#A8B0AA`, visible pero secundario.
- **Panel iOS:** es una tarjeta `#171E1A` con borde `#303832`, sale por encima del botón y tiene un ancho máximo de 280px.
- **Lo que está prohibido:** neón, brillos, pulsos llamativos y animaciones infinitas.
- **Z-index:** debe quedar por debajo de modales y offcanvas de Bootstrap (usa un valor < 1040).

---

## 8. Verificación (obligatoria antes de dar por terminado)

1. `dotnet build` sin errores ni nuevas advertencias.
2. En Chrome DevTools → **Application**:
   - Manifest sin errores e íconos cargando.
   - Service Worker `activated` con scope `/`.
   - Cache Storage que contenga solo `tas-*` y **ninguna** entrada de carrito, checkout, account, admin o `/api/`.
3. **Lighthouse → PWA/Installable:** debe cumplir los criterios de instalabilidad.
4. **Offline (DevTools → Network → Offline):**
   - una ruta visitada antes se abre correctamente;
   - una página no visitada muestra `offline.html`;
   - el carrito **no** carga desde la caché.
5. **Botón de instalar:**
   - aparece tras el retraso configurado y no aparece en modo standalone;
   - al descartarlo no reaparece al recargar;
   - en viewport móvil (375px) no tapa contenido ni otros botones fijos.
6. Incrementar `CACHE_VERSION` debe limpiar las cachés antiguas.

> **Nota:** el Service Worker requiere HTTPS. En desarrollo, `localhost` está permitido. En producción, confirma que el hosting sirve el sitio por HTTPS.

---

## 9. Reporte final

Al terminar, entrega:

- la lista de archivos creados y modificados;
- los prefijos de rutas sensibles que excluiste de la caché, con los nombres reales;
- la decisión tomada sobre la caché de páginas para usuarios autenticados;
- si los íconos son placeholders que deben reemplazarse;
- cualquier conflicto encontrado con elementos fijos existentes o con la configuración de `Program.cs`.
