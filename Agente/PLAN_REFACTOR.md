# Plan de orden y limpieza — Tren al Sur

> **Para Claude Code.** Lee este archivo completo antes de empezar.
> El proyecto es universitario y el dueño lo va a **estudiar después**. Manda la simplicidad sobre la elegancia.

## Contexto

- ASP.NET Core MVC (net10.0), EF Core + PostgreSQL (Supabase), Identity, Bootstrap/jQuery. Un solo proyecto (`EcommerceApp.csproj`).
- Ya funciona. **No se reconstruye desde cero**: se ordena, se corrige y se limpia.
- Este plan sale de un diagnóstico previo del código. Cada tarea indica el archivo donde está el problema.

## Reglas (valen en todas las fases)

1. **Código simple.** Sin repositorios, MediatR, AutoMapper, CQRS ni paquetes NuGet nuevos. Crea una clase nueva solo si elimina duplicación real. Nombres claros, LINQ básico y comentarios breves en español sobre cada acción o método (como ya hace el proyecto).
2. Respeta `Agente/AGENTS.md` y `Agente/.agent/*.md`. Si este plan los contradice, avísame antes de actuar.
3. **No inventes funcionalidades.** Si una tarea requiere una decisión de diseño, pregúntame.
4. **No toques:** cadenas de conexión, valores de Supabase/Google/GitHub, el framework, ni los `DataAnnotations` existentes (pueden ser requisito del docente). Nunca imprimas secretos ni el contenido de `appsettings.Development.json`.
5. **No apliques migraciones a la BD.** La BD es la de producción. Solo en la Fase 5 puedes *crear* archivos de migración, y **nunca** ejecutar `dotnet ef database update`; eso lo hago yo.
6. **No borres archivos sin mostrarme antes** la lista con la evidencia (`grep` de referencias) y esperar mi "sí".
7. Trabaja en la rama `refactor-orden`. Un commit por fase con mensaje claro. **No hagas push.**
8. Ahorra tokens: usa `grep` en vez de leer archivos enteros y no pegues archivos completos en pantalla.
9. **Al terminar cada fase:**
   - `dotnet build` con 0 errores.
   - Resumen corto de lo cambiado.
   - Lista de páginas que debo probar en el navegador (perfil `http`, puerto 5187, por los callbacks OAuth).
   - Marca las tareas como `[x]` en este archivo.
   - **Detente y espera mi OK.**

---

## Fase 0 — Preparación

- [x] Lee `Agente/AGENTS.md` y `Agente/.agent/*.md`; resume en 5 líneas lo que debes respetar.
  - **Nota:** ninguno de esos archivos existe en el repo (solo está `Agente/PLAN_REFACTOR.md`). No hay nada que resumir; avisado al usuario.
- [x] `git status`: confirma árbol limpio y crea la rama `refactor-orden`.
  - El árbol tenía cambios pendientes (`Data/AdminSeeder.cs`, `Program.cs` de una tarea anterior, y `Agente/` sin trackear). Se commitearon en `mejoras-diagnostico` (2 commits) antes de crear `refactor-orden`.
- [x] `dotnet build` de referencia (anota errores o warnings previos).
  - **0 errores, 5 warnings** `CS8619` (nulabilidad `List<string>` vs `List<string?>`) en `Controllers/ServicesController.cs:117` y `Controllers/ProductsController.cs:121-122`. No se tocan en esta fase.
- [x] `git ls-files`: confirma que no hay secretos versionados.
  - No hay ningún `appsettings*.json` trackeado; `appsettings.Development.json` existe solo en disco y está en `.gitignore`.

## Fase 1 — Seguridad y despliegue (Render)

- [x] `Data/AdminSeeder.cs`: quitar correo y contraseña del código. Leer `Admin:Email` y `Admin:Password` de configuración; si faltan, no crear nada. **No borres cuentas ya existentes** (puede ser la que usa el docente). Dime qué variables definir en Render (`Admin__Email`, `Admin__Password`) y en `user-secrets`.
  - Hecho en la tarea previa a este plan (commit `850cfac`). No borra cuentas existentes: si el email ya existe, solo asegura el rol Admin. Variables a definir: ver resumen de la fase.
- [x] `Program.cs`: agregar `UseForwardedHeaders` (X-Forwarded-For y X-Forwarded-Proto) antes de `UseHttpsRedirection`. En Render el proxy no es loopback, así que hay que limpiar `KnownNetworks` y `KnownProxies`; explícalo en un comentario. Sin esto, el login con Google/GitHub y los enlaces de "olvidé mi contraseña" salen con `http`.
  - Usé `KnownIPNetworks` en vez de `KnownNetworks` (esta última quedó obsoleta en net10.0, generaba warning `ASPDEPR005`).
- [x] `Program.cs`: ruta por defecto → `Home/Index` (hoy es `Account/Login`).
- [x] `AccountController.Login`: respetar `returnUrl` (solo si `Url.IsLocalUrl`) y usar `lockoutOnFailure: true`.
  - Se agregó `ReturnUrl` a `LoginViewModel` y un campo hidden en `Login.cshtml` para que viaje del GET (querystring, cuando `[Authorize]` redirige) al POST.
- [x] Crear `appsettings.Example.json` con las claves necesarias y valores vacíos. Confirmar que `.gitignore` excluye `appsettings.Development.json`.
  - Se mantuvieron algunos valores no sensibles como referencia (`Port`, `LogLevel`, `Bucket`, `From`); todo lo que es credencial quedó vacío.

## Fase 2 — Errores funcionales (sin cambiar el modelo de datos)

- [x] **Botón "AGREGAR" del catálogo falso** (`wwwroot/js/productos.js`, `agregarAlCarrito`): hoy solo cambia el texto y hace `console.log`. Debe hacer POST real a `Cart/Add` con el token antiforgery.
  - Mostrar "✓ AGREGADO" solo si el servidor confirma.
  - Si el usuario no ha iniciado sesión, redirigir al login.
  - Sin contador en la navbar (evita complejidad): basta el feedback del botón.
  - Detección de sesión no autenticada resuelta solo en JS (`response.redirected` → `window.location.href = response.url`), sin tocar `Program.cs`.
- [x] **Favoritos del catálogo** (`productos.js`): hoy usa `localStorage`. Cambiar a `FavoritesController.Toggle` y pintar el estado inicial con `FavoriteProductIds`, que ya llega a la vista. Eliminar el código de `localStorage`.
- [x] `Views/Products/Details.cshtml`: el input `#cantidad` está fuera del `<form>`, así que siempre se agrega 1. Ponerlo dentro del form como `name="quantity"`. Borrar las funciones JS muertas (`agregarAlCarrito`, `comprarAhora`, `toggleFavorito`).
- [x] `CartController.Add`: validar `quantity` (mínimo 1, máximo el stock disponible y 100), que el producto exista y tenga stock.
- [x] `FavoritesController.Toggle`: validar que `type` sea `Product` o `Service` y que el ítem exista.
- [x] Al borrar un producto o ruta, borrar también sus favoritos (`RemoveRange`).
- [x] **`CreatedAt` se pisa al editar** (`AdminController` `ProductEdit` y `ServiceEdit`): marcar `CreatedAt` como no modificado (`Entry(x).Property(p => p.CreatedAt).IsModified = false`).
  - **Nota:** `GuideEdit`/`TransportEdit` tienen el mismo bug (mismo patrón `Update(entity)` completo) pero no se tocaron: el plan solo menciona Product/Service. Queda pendiente si se quiere corregir en otra fase.
- [x] **`AdminController.cs` tenía texto corrupto** (`Gu├¡a`, `administraci├│n`, ~30 líneas). Corregido a UTF-8 correcto; verificado con `grep -n "├\|┬"` sin resultados.
- [x] `AdminController.ServiceDelete`: si la ruta tiene reservas, muestra un mensaje (`TempData["Success"]`, único toast wired en el layout admin) en vez de lanzar excepción, y no borra. `Service` no tiene `IsActive` como Guide/Transport, así que no se "desactiva": se bloquea el borrado.
- [x] `AdminController.ReservationUpdateStatus`: valida que `status` sea uno de `Pendiente|Recorrido|Acabado|Cancelado`.
- [x] **Moneda única:** creado `Helpers/PriceFormatter.cs` (`Format(decimal)`/`Format(decimal?)`) que formatea `"Bs. " + N2` con `InvariantCulture`. Reemplazados todos los `ToString("C")` y `"Bs."` sueltos en vistas vivas (se dejaron sin tocar `Views/Products/_ProductCard.cshtml` y `Views/Shared/_ServiceCardLarge.cshtml`: código muerto, candidatos a borrado en Fase 6).
- [x] `Views/Services/Details.cshtml`: reemplazado `onclick="alert('Sistema de reservas en desarrollo')"` por un botón deshabilitado "Reservas próximamente".
- [x] Voseo → tuteo en textos de usuario: corregido en vistas públicas y también en el panel Admin (decisión confirmada con el usuario: incluir Admin).

## Fase 3 — Un solo layout y estilos (visual: yo pruebo en el navegador)

- [x] `Views/Shared/_Layout.cshtml` único para la parte pública, con `lang="es"`, título "Tren al Sur", navbar y footer. Eliminar `<script type="importmap">` y las referencias a "EcommerceApp".
  - Navbar en un parcial `_Navbar.cshtml`. Enlaces: Inicio, Rutas, Productos, Favoritos, Carrito.
  - Usuario autenticado: "Hola, nombre" + botón "Cerrar sesión" (POST). Anónimo: "Iniciar sesión" y "Registrarse". Admin: enlace al panel.
  - **No** envolver `@RenderBody()` en `.container` (las secciones son a ancho completo).
  - Secciones `Styles` y `Scripts` para el CSS/JS de cada página.
  - **Nota:** se dejó el `<link>` a `~/EcommerceApp.styles.css` (bundle de CSS aislado que genera el propio SDK a partir de `_Layout.cshtml.css`, atado al nombre del `.csproj`, no es una marca visible); solo se quitó el texto "EcommerceApp" del `<title>`. El navbar es `position: fixed`, así que `<main>` tiene `padding-top` por defecto en `site.css`; las páginas con hero a pantalla completa (Home, Productos/Index, Servicios/Index, Servicios/Details) marcan `ViewData["FullBleed"] = true` para que el navbar transparente flote sobre su propio hero, igual que antes.
- [x] Quitar `Layout = null` y el HTML/navbar copiado de: `Home/Index`, `Products/Index`, `Products/Details`, `Services/Index`, `Services/Details`. Cada vista queda solo con su contenido.
  - De paso, en `Home/Index.cshtml` se quitó el `<footer class="footer-cinematic">` propio (duplicaba el footer del layout y tenía ~10 enlaces muertos: `#servicios`, `#contacto`, `#faq`, `#terminos`, `#privacidad`, redes sociales) y se corrigió el ítem "Carrito" de la barra móvil inferior, que apuntaba a `Products` en vez de `Cart`.
- [x] `Products/Details.cshtml` no tiene `<body>`; se resuelve con el layout. Esta vista no tiene hero a pantalla completa, así que no usa `FullBleed` (usa el espaciado normal para que el navbar no tape el breadcrumb).
- [x] Mover a `wwwroot/js/site.js` (una sola vez) el JS de scroll de la navbar y el menú móvil, hoy repetido en línea en cada vista.
  - Quitado de `home-new.js`, `servicios-new.js`, `servicios-detalle-new.js` y del script inline de `Products/Index.cshtml`/`Products/Details.cshtml`.
- [x] **Enlaces muertos:** eliminar los íconos `#search` y `#profile`; `#nosotros` solo si no existe esa sección.
  - Los tres desaparecieron junto con el navbar viejo (ninguna página tenía una sección `id="nosotros"` real). Se dejó sin tocar el `#profile` de la barra móvil inferior de Home (funcionalidad aparte, no mencionada en esta tarea, e inofensiva: no hay página de perfil todavía).
- [x] Cargar `home-new.css` solo en el Home (hoy se carga en todo el sitio desde `_Layout`).
- [x] Crear `wwwroot/css/tokens.css` con la paleta oficial y cargarlo primero en el layout:

  | Variable | Color |
  |---|---|
  | fondo | `#0B100E` |
  | sección | `#111713` |
  | tarjeta | `#171E1A` |
  | borde | `#303832` |
  | texto | `#F1F3F1` |
  | texto secundario | `#A8B0AA` |
  | verde | `#315D45` |
  | verde destacado | `#557F63` |
  | tierra | `#7C7058` |

  En `productos.css`, `carrito.css`, `favoritos.css` y `tren-al-sur.css`, apuntar sus variables locales a estas (`--productos-bg: var(--color-bg)`). Hoy usan valores parecidos pero distintos, como `#0D1210` o `#080d0b`. Edición mínima, sin reescribir los CSS.
  - **Nota:** `home-new.css`, `servicios-new.css` y `servicios-detalle-new.css` no se tocaron porque sus variables ya usaban exactamente estos valores (por eso no estaban en la lista de la tarea). `tren-al-sur.css` se carga desde `_AuthLayout.cshtml` (login/registro), que no cargaba ningún CSS de paleta; se le agregó el `<link>` a `tokens.css` para que sus `var(--color-*)` funcionen.
- [x] Añadir `@media (prefers-reduced-motion: reduce)` para desactivar animaciones, y `preload="metadata"` en el video del Home.
  - La regla quedó una sola vez en `tokens.css` (se carga en todas las páginas), en vez de repetirla en cada CSS.
- [x] Imagen por defecto: crear `wwwroot/images/default-product.svg` (SVG simple con el logo o un ícono de montaña) y usarla como único fallback. Reemplazar `/images/default-product.jpg` (no existe), `via.placeholder.com` y las URLs de Unsplash usadas como fallback.
  - Reemplazado en `Cart/Index`, `Favorites/Index`, `Home/Index`, `Products/Details`, `Products/_ProductoCard`, `Services/Index` y `Services/Details`.
  - **No** se tocaron `Views/Products/_ProductCard.cshtml` ni `Views/Shared/_ServiceCardLarge.cshtml`: son código muerto (nunca se renderizan, ver nota de Fase 2), candidatos a borrado en Fase 6.
  - **No** se tocó el arreglo `discoverPlaceholders` de `Home/Index` (rutas "PRÓXIMAMENTE" sin ruta real todavía): no es un fallback de imagen rota, es contenido decorativo para slots vacíos del carrusel.

**Build:** `dotnet build` → 0 errores (mismos 5 warnings preexistentes de Fase 0, no tocados). Probado también con `dotnet run` + `curl` sobre Home, Productos, Servicios, Cart, Favoritos, Login, `Products/Details/{id}` y `Services/Details/{id}`: todas devuelven 200 (Cart/Favoritos 302 a Login, es lo esperado sin sesión) y todos los CSS/JS/SVG nuevos resuelven con 200.

## Fase 4 — Categorías, ofertas y datos

**Decisión ya tomada:** la lista canónica es la que **ya está en la BD** (seeders), para no tocar datos en producción.

- [x] `Models/ProductCategories.cs`: reemplazar la lista actual (Hombre, Mujer, Niños, Ropa de abrigo, Equipo de cocina…) por las 10 categorías del `ProductSeeder`: Tiendas de campaña, Calzado de trekking, Mochilas, Bastones de trekking, Ropa outdoor, Camping, Hidratación, Iluminación, Accesorios, Seguridad y orientación.
  - Mover ahí el diccionario categoría → imagen que hoy está dentro de `Products/Index.cshtml`.
  - **Nota:** el diccionario de imágenes que ya estaba en `Products/Index.cshtml` usaba exactamente estos 10 nombres (nadie lo había actualizado tras el cambio de categorías), así que se movió tal cual a `ProductCategories.Images` sin reescribirlo.
- [x] `Models/Service.cs` → `RouteCategories.All`: dejar las categorías que usa `ServiceSeeder` (Senderismo, Trekking, Trekking con camping, Alta montaña, Caminatas de naturaleza). Corregir el comentario que dice "6" cuando hay otro número.
  - De paso se actualizó el diccionario categoría→imagen de `Home/Index.cshtml` (usaba las 8 categorías viejas) para que coincida con las 5 nuevas.
- [x] `ServicesController.Details`: mientras no exista la relación real (Fase 5), que "equipamiento recomendado" use las categorías correctas (`Mochilas`, `Calzado de trekking`, `Tiendas de campaña`, `Iluminación`). Hoy busca "Bolsos y mochilas" y "Equipamiento", que no existen en los datos.
- [x] **Filtro de ofertas** en `ProductsController.Index` y en `Products/Index.cshtml`: parámetro `onlyOffers` y casilla en el formulario de filtros. Oferta = `PromotionalPrice != null && PromotionalPrice < Price`.
  - Se reutilizó la clase CSS `.productos-filters__checkbox`, que ya existía en `productos.css` pero no se usaba en ningún lado.
- [x] **Eliminar `GetFilteredProducts` y los `GetFilterOptions`** de `ProductsController` y `ServicesController`: ningún JS los llama y `GetFilteredProducts` apunta a una vista inexistente (`_ProductGrid`). Con esto desaparece la duplicación de filtros.
  - Confirmado con `grep` sobre `.js`/`.cshtml`: cero referencias a `GetFilteredProducts`, `GetFilterOptions` o `_ProductGrid` antes de borrar.
- [x] `HomeController`: quitar los `CommunityPosts` inventados (`@montañista`, `@trekker`, fotos de Unsplash). En la vista, mostrar esa sección solo si la lista tiene elementos.
  - **Nota:** `Home/Index.cshtml` nunca renderizaba `Model.CommunityPosts` (no existe ninguna sección de comunidad en la vista); era una lista fabricada en el controlador que no se mostraba en ningún lado. Se quitó la fabricación; la propiedad `CommunityPosts` del ViewModel queda en su valor por defecto (lista vacía) para cuando exista contenido real. No había ninguna sección que "mostrar solo si tiene elementos": no hace falta tocar la vista.
- [x] `Home/Index.cshtml`: los marcadores del mapa usan `data-route-id` fijos (1, 2, 3). Generarlos desde `Model.RouteMarkers` con los ids reales.
  - Las posiciones (top/left %) siguen siendo fijas (el mapa es un placeholder visual, no hay coordenadas reales todavía, según el propio comentario del código: "preparada para integración con Leaflet/Google Maps"); se armó un arreglo de 10 posiciones que se recorre por índice para no repetir el layout anterior de 3 puntos fijos.

**Build:** `dotnet build --no-incremental` → 0 errores (mismos 5 warnings preexistentes de Fase 0, no tocados). Probado con `dotnet run` (perfil `http`, puerto 5187) + `curl`: Home, Productos, Servicios, `Products/Details/{id real}`, `Services/Details/{id real}` devuelven 200; Cart/Favoritos 302 a Login (esperado sin sesión); `Products?onlyOffers=true` devuelve 200; el filtro de categorías de Productos ya muestra las 10 categorías nuevas y el mapa del Home ya muestra `data-route-id` con ids reales de la BD (no 1/2/3).

## Fase 5 — Base de datos (**pídeme confirmación antes de empezar**)

Solo crear migraciones, **nunca aplicarlas**. Al terminar, muéstrame el SQL con `dotnet ef migrations script`.

- [x] **Relación ruta → equipamiento:** tabla puente `ServiceProduct` (`ServiceId`, `ProductId`, clave compuesta). En el formulario admin de la ruta, un multi-select de productos. En `Services/Details` mostrar solo los productos asignados y ocultar la sección si no hay. Quitar la consulta genérica y el `// TODO`.
  - `Models/ServiceProduct.cs` nuevo. FKs con `OnDelete(DeleteBehavior.Cascade)` en `ApplicationDbContext`: si se borra la ruta o el producto, el vínculo desaparece solo (no hace falta borrarlo a mano, a diferencia de `FavoriteItem` que no tiene FK real).
  - `AdminController.LoadServiceLookupsAsync` ahora también carga `ViewBag.Products`; `ServiceCreate`/`ServiceEdit` (GET y POST) manejan un nuevo parámetro `List<int>? productIds` y sincronizan `ServiceProducts` (en Edit: borra los vínculos existentes y crea los nuevos, mismo patrón que ya usaba el borrado de favoritos).
  - `ServiceCreate.cshtml`/`ServiceEdit.cshtml`: sección nueva "Equipamiento recomendado" con `<select multiple>` de productos (sin JS adicional, mismo estilo simple que Guía/Transporte).
  - `ServicesController.Details`: reemplazada la consulta por categorías fijas (`Mochilas`, `Calzado de trekking`...) por la relación real vía `context.ServiceProducts`. Se quitó el `// TODO`. La vista (`Services/Details.cshtml`) ya ocultaba la sección si `RecommendedProducts` estaba vacío, así que no necesitó cambios.
- [x] **Galería de producto:** campo `GalleryImages` en `Product`, separado por `|` como en `Service`. Campo en el formulario admin y miniaturas en `Products/Details` (producto, en uso, detalle, contexto).
  - Reutilizados los helpers existentes `UploadGalleryAsync`/`AppendPipeSeparated` (ya usados por Service) en `ProductCreate`/`ProductEdit` del `AdminController`.
- [x] **No tocar** la clave primaria de `Reservation` ni implementar reservas reales todavía. Anotado en `Agente/.agent/TASKS.md` (nuevo): la PK `(UserId, ServiceId)` permite una sola reserva por ruta y no hay fecha de salida.
- [x] **No borrar** la migración vacía `AddCartAndFavorites2`: no se tocó.
  - Migración creada: `20260921053404_AddProductGalleryAndServiceProducts` (una sola migración: EF Core calculó ambos cambios juntos porque no hay ninguna aplicada desde la última). SQL completo abajo.
  - **No se ejecutó `dotnet ef database update`** ni ninguna operación de escritura sobre la BD de producción. `dotnet ef migrations remove`/`add`/`script` sí hacen una consulta de solo lectura a `__EFMigrationsHistory` para saber qué migraciones ya están aplicadas (comportamiento estándar de la herramienta, no fue una acción explícita); no se modificó ni se leyó ningún otro dato.

**SQL de la migración nueva** (`dotnet ef migrations script AddCartAndFavorites2 AddProductGalleryAndServiceProducts`):

```sql
START TRANSACTION;
ALTER TABLE "Products" ADD "GalleryImages" character varying(2000);

CREATE TABLE "ServiceProducts" (
    "ServiceId" integer NOT NULL,
    "ProductId" integer NOT NULL,
    CONSTRAINT "PK_ServiceProducts" PRIMARY KEY ("ServiceId", "ProductId"),
    CONSTRAINT "FK_ServiceProducts_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ServiceProducts_Services_ServiceId" FOREIGN KEY ("ServiceId") REFERENCES "Services" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ServiceProducts_ProductId" ON "ServiceProducts" ("ProductId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260921053404_AddProductGalleryAndServiceProducts', '10.0.0');

COMMIT;
```

**Build:** `dotnet build --no-incremental` → 0 errores (mismos 5 warnings preexistentes, no tocados).

**Importante — no se probó en el navegador esta fase.** La migración solo existe como archivo, no se aplicó a la BD de producción (regla 5). Como el código ya consulta la columna `Products.GalleryImages` y la tabla `ServiceProducts` en cada página que carga productos o el detalle de una ruta, **cualquier intento de correr la app contra la BD actual va a fallar** (columna/tabla inexistente) hasta que corras tú `dotnet ef database update`. Una vez aplicada la migración, probar: Productos (listado y detalle), Rutas (detalle, ver equipamiento recomendado), Admin → Productos (crear/editar, campo de galería) y Admin → Rutas (crear/editar, selector de equipamiento).

## Fase 6 — Limpieza de archivos (**muestra la lista y espera mi "sí"**)

Antes de borrar cada archivo, haz `grep` y muéstrame que nadie lo referencia.

- [x] `Dockerfile.cs` (una clase vacía creada por error) y la carpeta vacía `-Force/`.
  - Confirmado sin referencias en `.csproj`, `.cs` ni en el `Dockerfile` real (solo copia `*.csproj` y hace `dotnet publish`). `-Force/` estaba vacía. Ambos borrados.
- [x] `wwwroot/css/home.css`, `wwwroot/css/servicios.css`, `wwwroot/js/home.js`, `wwwroot/js/servicios.js`: versiones antiguas, hoy no referenciadas por ninguna vista.
  - Confirmado con `grep` de `<link>`/`<script src>` en todo `Views/`: 0 resultados. Borrados.
- [x] `Views/Products/_ProductCard.cshtml` y `Views/Shared/_ServiceCardLarge.cshtml`: nunca se renderizan (solo se usa `_ProductoCard`).
  - Confirmado: la única partial de producto usada es `_ProductoCard` (con "o"), en `Views/Products/Index.cshtml:166`. Cero `Html.Partial`/`<partial>`/`PartialView` apuntando a `_ProductCard` o `_ServiceCardLarge`. Borrados.
- [x] Revisar `tren-al-sur.css` y `Views/Shared/_Layout.cshtml.css`: borrarlos solo si nada los usa tras la Fase 3.
  - `tren-al-sur.css` **sigue en uso** (`Views/Shared/_AuthLayout.cshtml:12` y `Views/Admin/_AdminLayout.cshtml:32`): no se tocó.
  - `Views/Shared/_Layout.cshtml.css` (scaffold de plantilla: `.navbar-brand`, `.btn-primary`, `.nav-pills`, `.footer`) ya no matchea ninguna clase del `_Layout.cshtml` actual (usa `_Navbar` con `.nav-logo`/`.nav-menu` y `<footer class="site-footer">`). Decisión confirmada con el usuario: se borró. **Pendiente que el usuario confirme visualmente** navbar/botones/footer en el navegador.
- [x] Renombrar los `*-new` (`home-new.css`, `servicios-new.css`, `servicios-detalle-new.css`, `home-new.js`, `servicios-new.js`, `servicios-detalle-new.js`) quitando el sufijo, y actualizar las referencias.
  - Renombrados con `git mv` a `home.css`, `servicios.css`, `servicios-detalle.css`, `home.js`, `servicios.js`, `servicios-detalle.js`. Actualizadas las 6 referencias en `Views/Home/Index.cshtml`, `Views/Services/Index.cshtml` y `Views/Services/Details.cshtml`.
- [x] Buscar y eliminar código comentado, `TODO` obsoletos y `using` sin uso.
  - Revisado `Controllers/*.cs` y `Program.cs`: sin bloques de código comentado (solo comentarios explicativos legítimos en español), sin `TODO`/`FIXME` reales, y build con analizador `IDE0005` (usings innecesarios) → 0 warnings. Nada que limpiar.

## Fase 7 — Orden interno y material de estudio

- [x] Dividir `AdminController` (34 KB) en **clases parciales por entidad**: `AdminController.cs` (base, dashboard, subida de imágenes), `AdminController.Products.cs`, `.Services.cs`, `.Guides.cs`, `.Transports.cs`, `.Reservations.cs`. Misma clase y mismas rutas; solo se separan archivos.
  - El constructor primario (`context`, `imageStorage`) queda solo en `AdminController.cs`; las demás partes son `partial class AdminController` sin parámetros (válido en C# con constructores primarios + partial classes). `dotnet build` → 0 errores, mismos 5 warnings preexistentes.
- [x] Crear `GUIA_DE_ESTUDIO.md` (máximo 2 páginas, en español):
  - Qué hace cada carpeta.
  - Diagrama de texto de las entidades y sus relaciones.
  - Flujo de una petición: ruta → controlador → vista, con un ejemplo real (catálogo de productos).
  - Cómo funciona el login (Identity, roles, Google/GitHub).
  - Cómo correr el proyecto y cómo se despliega en Render, con **nombres** de variables de entorno, nunca valores.
  - Creado en `Agente/GUIA_DE_ESTUDIO.md`.
- [x] Actualizar `Agente/.agent/PROJECT_STATE.md`, `TASKS.md` y `MEMORY.md` con el estado real: 8 migraciones (no 7; se contaron los archivos reales en `Migrations/`), sin AJAX, un solo layout, etc. Corregir "Argentina" → Bolivia. No incluyas correos ni credenciales en esos `.md`.
  - `PROJECT_STATE.md` y `MEMORY.md` no existían (solo `TASKS.md`, creado en Fase 5); se crearon ambos. `TASKS.md` seguía vigente, no se modificó. No había ningún texto real con "Argentina" en el repo (la corrección del plan era preventiva); se verificó con `grep -rn Argentina` antes de escribir los `.md` nuevos.
- [x] Revisión final: `dotnet build` con 0 errores, `git status` limpio, y un resumen de todo lo que quedó pendiente.
