# Tren al Sur — lista de correcciones pendientes (v2)

Reemplaza la versión anterior del documento. Se agregó el diagnóstico de
Mis pedidos / Mis reservas / Carrito / Favoritos (antes pendiente de
captura) y se completaron las categorías de Rutas.

**Regla general: después de cada fix, correr `dotnet watch` y verificar
visualmente en el navegador (con hard refresh / caché deshabilitada en
DevTools) antes de pasar al siguiente punto.** Varios de estos bugs pueden
estar relacionados entre sí (ver Prioridad 1) — no asumas que un síntoma
necesita su propio fix independiente sin volver a revisarlo primero.

---

## PRIORIDAD 1 — causa raíz probable de varios síntomas del Home

**Archivo:** `wwwroot/css/home.css`, selector `.hero-brand__media`

Hoy usa `position: fixed; inset: 0; z-index: -1;` para que el video del hero
quede "clavado" mientras se hace scroll, esperando que las siguientes
secciones (con fondo sólido) lo taparan al pasar por encima.

**Problema:** `position: fixed` ancla el elemento al viewport de *toda la
página*, no solo a la sección del hero. En capturas del usuario se ve el
fondo de montaña filtrándose en secciones muy posteriores (productos
curados, sección emocional del final).

**Fix:** cambiar `.hero-brand__media` a `position: sticky; top: 0;`, sacando
el `z-index: -1` (dejar `z-index: 0`, con `.hero-brand__content` en
`z-index: 2` como ya está). `position: sticky` se "pega" al hacer scroll
solo dentro de los límites de `.hero-brand` (que ya es `position: relative`)
— apenas termina esa sección, la siguiente la tapa de forma garantizada, sin
fugas al resto de la página.

**Después de este fix, volver a revisar en el navegador (antes de tocar nada
más) estos tres puntos, porque es probable que se resuelvan solos:**
- Títulos que "no aparecen" en Rutas destacadas / Categorías / Equipamiento
  curado.
- El footer "no aparece" al final del Home.
- El hover raro en las tarjetas de productos curados del Home.

Si alguno persiste después de este fix, recién ahí investigarlo aparte.

---

## PRIORIDAD 2 — bugs de autenticación (causa confirmada)

### 2.1 — Logout manda a Productos en vez de Home
**Archivo:** `Controllers/AccountController.cs`, acción `Logout`
Cambiar `return RedirectToAction("Index", "Products");` por
`return RedirectToAction("Index", "Home");`

### 2.2 — "Agregar al carrito" y "Favorito" sin sesión no hacen nada
**Archivo:** `wwwroot/js/productos.js`, funciones `initFavorites` y
`agregarAlCarritoConCantidad`

**Causa confirmada:** ambas peticiones mandan `X-Requested-With:
XMLHttpRequest`. ASP.NET Identity, al ver ese header en un usuario sin
sesión, devuelve `401 Unauthorized` en vez de redirigir a Login. El código
actual solo revisa `response.redirected` (da `false`) y llama a
`response.json()`, que falla en silencio.

**Fix:** en ambas funciones, revisar `response.status === 401` antes de
`response.json()` y redirigir manualmente a
`/Account/Login?ReturnUrl=` + `encodeURIComponent(window.location.pathname)`.

---

## PRIORIDAD 3 — Mis pedidos / Mis reservas / Carrito / Favoritos "deformes"

**Diagnóstico confirmado con captura** (ya no hace falta pedir más fotos de
esto). Afecta a las 4 páginas por igual — son el mismo patrón de código:

1. **No hay fondo oscuro global.** `body`/`html` nunca tienen
   `background-color` en `site.css`. Home, Productos y Rutas lo resuelven
   cada uno por su cuenta con un `<style>body{background:#0B100E}</style>`
   metido en su propia vista — pero Cart, Favorites, Orders, Reservations
   (y probablemente Account/Login) nunca lo tuvieron, así que se ve blanco
   del navegador alrededor del contenido.

   **Fix:** agregar UNA sola vez, global, en `wwwroot/css/site.css`:
   `html, body { background-color: var(--color-bg); }` (usando la variable
   de `tokens.css`). Así se resuelve para estas 4 páginas y para cualquier
   vista futura, sin tener que acordarse de repetirlo por página. Los
   `<style>` inline en Home/Productos/Servicios pueden quedar (son
   redundantes pero inofensivos) o limpiarse ya que con el fix global sobran.

2. **El contenido usa `<div class="container">` de Bootstrap** (ancho
   máximo fijo, centrado), mientras que el resto del sitio ya rediseñado
   usa contenedores propios más anchos (`.container-fluid`, max-width
   1400px). Por eso se ve angosto con gutters grandes a los costados,
   inconsistente con Home/Productos/Rutas.

   **Archivos:** `Views/Cart/Index.cshtml`, `Views/Favorites/Index.cshtml`,
   `Views/Orders/Index.cshtml`, `Views/Reservations/Index.cshtml` (y
   revisar `Views/Orders/Details.cshtml` si existe el mismo patrón).

   **Fix:** cambiar `class="container cart-page"` / `class="container
   fav-page"` por algo como `class="container-fluid cart-page"` y agregar en
   `carrito.css`/`favoritos.css` un `max-width: 1200px; margin: 0 auto;
   padding: 0 2rem;` sobre `.cart-page`/`.fav-page` mismos (en vez de
   depender del `.container` de Bootstrap), para mantener el contenido
   centrado pero con el ancho premium consistente con el resto del sitio.

Nota: el usuario había aprobado Carrito y Favoritos como "perfecto" en una
revisión anterior — es el mismo bug, simplemente no se notó en esa revisión.
Aplicar el fix a las 4 páginas por igual, no solo a Pedidos/Reservas.

**Revisar también `Views/Account/Login.cshtml` y `Register.cshtml`** (usan
`_AuthLayout.cshtml`, un layout separado) por si tienen el mismo problema de
fondo blanco — confirmar visualmente.

---

## HOME

### 4.1 — Fondo detrás del video
Una vez aplicado el fix de Prioridad 1, fuera del hero el fondo debe ser
color sólido de la paleta (`var(--home-bg)`), no el video. Confirmar
visualmente.

### 4.2 — Footer y 4.3 — Hover de productos curados
Confirmar después del fix de Prioridad 1 (ver esa sección para el detalle).

---

## PRODUCTOS

### 5.1 — Categorías: completar todas las que existan
**Archivo:** `Models/Product.cs`, clase `ProductCategories`
Revisar `ProductCategories.Images` contra los valores reales de
`Product.Category` en la base. Completar con imagen cualquier categoría que
falte (hoy cae al `DefaultImage` si no está mapeada). El brief original
define 9: Mochilas, Calzado, Camping, Ropa Outdoor, Hidratación,
Iluminación, Bastones, Seguridad, Accesorios.

### 5.2 — Compactar la grilla de categorías (mosaico)
**Archivo:** `wwwroot/css/productos.css`, `.productos-categories__grid`
Reducir `grid-auto-rows` (hoy 130px) y/o el `gap`, y limitar cuántas filas
puede ocupar una categoría, para que la sección de categorías no obligue a
tanto scroll antes de llegar al catálogo.

### 5.3 — Filtros: pestaña estática (sticky) al hacer scroll
**Archivo:** `wwwroot/css/productos.css`, `.productos-filters`
Agregar `position: sticky; top: 90px;` (ajustar `top` a la altura real del
navbar) y `max-height: calc(100vh - 100px); overflow-y: auto;`.

### 5.4 — Filtros: mejorar las líneas/bordes laterales
**Archivo:** `wwwroot/css/productos.css`
Cambiar el `border` genérico blanco de `.productos-filters` y de cada grupo
por `var(--productos-border)` en un tono más sutil, o separar los grupos con
un borde inferior fino en vez de recuadros completos.

---

## RUTAS (Servicios)

### 6.1 — Hero rotativo: controles más discretos
**Archivo:** `wwwroot/css/servicios.css`, `.rutas-hero__controls`
El autoplay cada 7s ya funciona. Las flechas y el botón play/pausa hoy son
círculos con borde bien visible. Bajarles la opacidad en reposo (`opacity:
0.4`) y subirla solo con hover sobre esa zona del hero
(`.rutas-hero:hover .rutas-hero__controls { opacity: 1; }`).

### 6.2 — Categorías de rutas: completar (ya confirmadas por el usuario)
**Archivo:** `Models/Service.cs`, clase `RouteCategories`
`RouteCategories.All` hoy tiene 5: Senderismo, Trekking, Trekking con
camping, Alta montaña, Caminatas de naturaleza.
**Agregar estas 3** (confirmadas):
- Aventura extrema
- Volcanes y montañas
- Escalada

Total: 8 categorías. Agregarlas también al diccionario de imágenes de
categorías de rutas si existe uno equivalente a `ProductCategories.Images`
(revisar si `Service`/`RouteCategories` tiene su propio mapeo de imagen por
categoría; si no existe, puede que haga falta crearlo para que se vean bien
en el listado/filtro).

### 6.3 — Compactar filtros
**Archivo:** `wwwroot/css/servicios.css`, `.servicios-filters-section` /
`.servicios-filters`
Reducir padding/gap entre los grupos de filtros (Ubicación, Dificultad,
Categoría, Duración). Considerar ponerlos en fila horizontal con wrap en vez
de bloques apilados.

### 6.4 — Mapa funcional en el Home
El mapa oscuro con Leaflet (`.rutas-map-section` + `initRutasMap()` en
`servicios.js`) se había sacado del Home para no duplicar. Recuperarlo
también ahí ("mapa funcional igual al que había antes, donde se ven las
rutas cercanas"), reutilizando `initRutasMap()` en vez de duplicar código —
revisar qué trae `HomeController`/su ViewModel hoy para las coordenadas de
rutas antes de armar la sección.

---

## DETALLE DE RUTA

### 7.1 — Mapa funcional
**Archivo:** `Views/Services/Details.cshtml`
Agregar la misma sección de mapa oscuro (Leaflet + CartoDB dark) mostrando
la ubicación de esa ruta puntual (`StartLatitude/StartLongitude`, y
`EndLatitude/EndLongitude` si corresponde marcar inicio y fin). Reutilizar
`initRutasMap()` de `servicios.js` en vez de duplicar la lógica — considerar
extraerla a un archivo JS compartido si se va a usar en Home + Rutas +
Detalle de ruta.

---

## PANEL DE ADMINISTRACIÓN

### 8.1 — Tema en verde, no blanco
**Archivo:** `wwwroot/css/tren-al-sur.css` (variables `--forest-*`,
`--sand`, `--bg-primary`, etc.)
Revisar si estas variables realmente apuntan a la paleta oscura (`#0B100E`,
`#111713`, `#315D45`...) o si quedaron con valores claros por defecto, y si
`tren-al-sur.css` está cargando correctamente en `_AdminLayout.cshtml`.

---

## NAVBAR

### 9.1 — Buscador estático, botones de sesión más compactos
**Archivo:** `Views/Shared/_Navbar.cshtml` + `wwwroot/css/site.css`
Cambiar `.nav-search` de "ícono que despliega input al hacer clic" a input
siempre visible. Para hacerle lugar, comprimir "Iniciar sesión" /
"Registrarse" (menos padding, o solo ícono en pantallas medianas con texto
completo solo en desktop grande). El JS `initNavSearch` en `site.js` puede
simplificarse o quedar solo para el comportamiento mobile.

---

## Resumen de orden sugerido

1. Prioridad 1 (video sticky) → volver a mirar el Home completo.
2. Prioridad 2 (logout, 401 en AJAX).
3. Prioridad 3 (fondo oscuro global + contenedor consistente en
   Pedidos/Reservas/Carrito/Favoritos/Account).
4. Home: confirmar 4.1/4.2/4.3 después del punto 1.
5. Productos (categorías completas + compactar + filtros sticky + bordes).
6. Rutas (controles discretos + 3 categorías nuevas + filtros compactos +
   mapa en Home).
7. Detalle de ruta (mapa).
8. Admin (paleta verde).
9. Navbar (buscador estático).
