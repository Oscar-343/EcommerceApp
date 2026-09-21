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

- [ ] Lee `Agente/AGENTS.md` y `Agente/.agent/*.md`; resume en 5 líneas lo que debes respetar.
- [ ] `git status`: confirma árbol limpio y crea la rama `refactor-orden`.
- [ ] `dotnet build` de referencia (anota errores o warnings previos).
- [ ] `git ls-files`: confirma que no hay secretos versionados.

## Fase 1 — Seguridad y despliegue (Render)

- [ ] `Data/AdminSeeder.cs`: quitar correo y contraseña del código. Leer `Admin:Email` y `Admin:Password` de configuración; si faltan, no crear nada. **No borres cuentas ya existentes** (puede ser la que usa el docente). Dime qué variables definir en Render (`Admin__Email`, `Admin__Password`) y en `user-secrets`.
- [ ] `Program.cs`: agregar `UseForwardedHeaders` (X-Forwarded-For y X-Forwarded-Proto) antes de `UseHttpsRedirection`. En Render el proxy no es loopback, así que hay que limpiar `KnownNetworks` y `KnownProxies`; explícalo en un comentario. Sin esto, el login con Google/GitHub y los enlaces de "olvidé mi contraseña" salen con `http`.
- [ ] `Program.cs`: ruta por defecto → `Home/Index` (hoy es `Account/Login`).
- [ ] `AccountController.Login`: respetar `returnUrl` (solo si `Url.IsLocalUrl`) y usar `lockoutOnFailure: true`.
- [ ] Crear `appsettings.Example.json` con las claves necesarias y valores vacíos. Confirmar que `.gitignore` excluye `appsettings.Development.json`.

## Fase 2 — Errores funcionales (sin cambiar el modelo de datos)

- [ ] **Botón "AGREGAR" del catálogo falso** (`wwwroot/js/productos.js`, `agregarAlCarrito`): hoy solo cambia el texto y hace `console.log`. Debe hacer POST real a `Cart/Add` con el token antiforgery.
  - Mostrar "✓ AGREGADO" solo si el servidor confirma.
  - Si el usuario no ha iniciado sesión, redirigir al login.
  - Sin contador en la navbar (evita complejidad): basta el feedback del botón.
- [ ] **Favoritos del catálogo** (`productos.js`): hoy usa `localStorage`. Cambiar a `FavoritesController.Toggle` y pintar el estado inicial con `FavoriteProductIds`, que ya llega a la vista. Eliminar el código de `localStorage`.
- [ ] `Views/Products/Details.cshtml`: el input `#cantidad` está fuera del `<form>`, así que siempre se agrega 1. Ponerlo dentro del form como `name="quantity"`. Borrar las funciones JS muertas (`agregarAlCarrito`, `comprarAhora`, `toggleFavorito`).
- [ ] `CartController.Add`: validar `quantity` (mínimo 1, máximo el stock disponible y 100), que el producto exista y tenga stock.
- [ ] `FavoritesController.Toggle`: validar que `type` sea `Product` o `Service` y que el ítem exista.
- [ ] Al borrar un producto o ruta, borrar también sus favoritos (`RemoveRange`).
- [ ] **`CreatedAt` se pisa al editar** (`AdminController` `ProductEdit` y `ServiceEdit`): marcar `CreatedAt` como no modificado (`Entry(x).Property(p => p.CreatedAt).IsModified = false`).
- [ ] **`AdminController.cs` tiene texto corrupto** (`Gu├¡a`, `administraci├│n`, unas 30 líneas). Corregir los textos a UTF-8 correcto y verificar con `grep -n "├\|┬"` que no queda ninguno.
- [ ] `AdminController.ServiceDelete`: si la ruta tiene reservas, mostrar un mensaje en vez de lanzar excepción (FK `Restrict`). Sigue el patrón de guías y transportes.
- [ ] `AdminController.ReservationUpdateStatus`: validar que `status` sea uno de `Pendiente|Recorrido|Acabado|Cancelado`.
- [ ] **Moneda única:** crear un helper mínimo (por ejemplo `Helpers/PriceFormatter.cs`) que formatee `Bs. ` + número con `InvariantCulture`. Reemplazar todos los `ToString("C")` y los `"Bs."` sueltos en vistas. `ToString("C")` depende de la cultura del servidor y en Render sale con `$` o `¤`.
- [ ] `Views/Services/Details.cshtml`: reemplazar `onclick="alert('Sistema de reservas en desarrollo')"` por un botón deshabilitado "Reservas próximamente". **No implementes reservas.**
- [ ] Voseo → tuteo en textos de usuario: buscar "Hacé", "Definí", "AHORRÁS" y similares.

## Fase 3 — Un solo layout y estilos (visual: yo pruebo en el navegador)

- [ ] `Views/Shared/_Layout.cshtml` único para la parte pública, con `lang="es"`, título "Tren al Sur", navbar y footer. Eliminar `<script type="importmap">` y las referencias a "EcommerceApp".
  - Navbar en un parcial `_Navbar.cshtml`. Enlaces: Inicio, Rutas, Productos, Favoritos, Carrito.
  - Usuario autenticado: "Hola, nombre" + botón "Cerrar sesión" (POST). Anónimo: "Iniciar sesión" y "Registrarse". Admin: enlace al panel.
  - **No** envolver `@RenderBody()` en `.container` (las secciones son a ancho completo).
  - Secciones `Styles` y `Scripts` para el CSS/JS de cada página.
- [ ] Quitar `Layout = null` y el HTML/navbar copiado de: `Home/Index`, `Products/Index`, `Products/Details`, `Services/Index`, `Services/Details`. Cada vista queda solo con su contenido.
- [ ] `Products/Details.cshtml` no tiene `<body>`; se resuelve con el layout.
- [ ] Mover a `wwwroot/js/site.js` (una sola vez) el JS de scroll de la navbar y el menú móvil, hoy repetido en línea en cada vista.
- [ ] **Enlaces muertos:** eliminar los íconos `#search` y `#profile`; `#nosotros` solo si no existe esa sección.
- [ ] Cargar `home-new.css` solo en el Home (hoy se carga en todo el sitio desde `_Layout`).
- [ ] Crear `wwwroot/css/tokens.css` con la paleta oficial y cargarlo primero en el layout:

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
- [ ] Añadir `@media (prefers-reduced-motion: reduce)` para desactivar animaciones, y `preload="metadata"` en el video del Home.
- [ ] Imagen por defecto: crear `wwwroot/images/default-product.svg` (SVG simple con el logo o un ícono de montaña) y usarla como único fallback. Reemplazar `/images/default-product.jpg` (no existe), `via.placeholder.com` y las URLs de Unsplash usadas como fallback.

## Fase 4 — Categorías, ofertas y datos

**Decisión ya tomada:** la lista canónica es la que **ya está en la BD** (seeders), para no tocar datos en producción.

- [ ] `Models/ProductCategories.cs`: reemplazar la lista actual (Hombre, Mujer, Niños, Ropa de abrigo, Equipo de cocina…) por las 10 categorías del `ProductSeeder`: Tiendas de campaña, Calzado de trekking, Mochilas, Bastones de trekking, Ropa outdoor, Camping, Hidratación, Iluminación, Accesorios, Seguridad y orientación.
  - Mover ahí el diccionario categoría → imagen que hoy está dentro de `Products/Index.cshtml`.
- [ ] `Models/Service.cs` → `RouteCategories.All`: dejar las categorías que usa `ServiceSeeder` (Senderismo, Trekking, Trekking con camping, Alta montaña, Caminatas de naturaleza). Corregir el comentario que dice "6" cuando hay otro número.
- [ ] `ServicesController.Details`: mientras no exista la relación real (Fase 5), que "equipamiento recomendado" use las categorías correctas (`Mochilas`, `Calzado de trekking`, `Tiendas de campaña`, `Iluminación`). Hoy busca "Bolsos y mochilas" y "Equipamiento", que no existen en los datos.
- [ ] **Filtro de ofertas** en `ProductsController.Index` y en `Products/Index.cshtml`: parámetro `onlyOffers` y casilla en el formulario de filtros. Oferta = `PromotionalPrice != null && PromotionalPrice < Price`.
- [ ] **Eliminar `GetFilteredProducts` y los `GetFilterOptions`** de `ProductsController` y `ServicesController`: ningún JS los llama y `GetFilteredProducts` apunta a una vista inexistente (`_ProductGrid`). Con esto desaparece la duplicación de filtros.
- [ ] `HomeController`: quitar los `CommunityPosts` inventados (`@montañista`, `@trekker`, fotos de Unsplash). En la vista, mostrar esa sección solo si la lista tiene elementos.
- [ ] `Home/Index.cshtml`: los marcadores del mapa usan `data-route-id` fijos (1, 2, 3). Generarlos desde `Model.RouteMarkers` con los ids reales.

## Fase 5 — Base de datos (**pídeme confirmación antes de empezar**)

Solo crear migraciones, **nunca aplicarlas**. Al terminar, muéstrame el SQL con `dotnet ef migrations script`.

- [ ] **Relación ruta → equipamiento:** tabla puente `ServiceProduct` (`ServiceId`, `ProductId`, clave compuesta). En el formulario admin de la ruta, un multi-select de productos. En `Services/Details` mostrar solo los productos asignados y ocultar la sección si no hay. Quitar la consulta genérica y el `// TODO`.
- [ ] **Galería de producto:** campo `GalleryImages` en `Product`, separado por `|` como en `Service`. Campo en el formulario admin y miniaturas en `Products/Details` (producto, en uso, detalle, contexto).
- [ ] **No tocar** la clave primaria de `Reservation` ni implementar reservas reales todavía. Solo déjalo anotado en `TASKS.md`: la PK `(UserId, ServiceId)` permite una sola reserva por ruta y no hay fecha de salida.
- [ ] **No borres** la migración vacía `AddCartAndFavorites2`: puede estar registrada en `__EFMigrationsHistory`.

## Fase 6 — Limpieza de archivos (**muestra la lista y espera mi "sí"**)

Antes de borrar cada archivo, haz `grep` y muéstrame que nadie lo referencia.

- [ ] `Dockerfile.cs` (una clase vacía creada por error) y la carpeta vacía `-Force/`.
- [ ] `wwwroot/css/home.css`, `wwwroot/css/servicios.css`, `wwwroot/js/home.js`, `wwwroot/js/servicios.js`: versiones antiguas, hoy no referenciadas por ninguna vista.
- [ ] `Views/Products/_ProductCard.cshtml` y `Views/Shared/_ServiceCardLarge.cshtml`: nunca se renderizan (solo se usa `_ProductoCard`).
- [ ] Revisar `tren-al-sur.css` y `Views/Shared/_Layout.cshtml.css`: borrarlos solo si nada los usa tras la Fase 3.
- [ ] Renombrar los `*-new` (`home-new.css`, `servicios-new.css`, `servicios-detalle-new.css`, `home-new.js`, `servicios-new.js`, `servicios-detalle-new.js`) quitando el sufijo, y actualizar las referencias.
- [ ] Buscar y eliminar código comentado, `TODO` obsoletos y `using` sin uso.

## Fase 7 — Orden interno y material de estudio

- [ ] Dividir `AdminController` (34 KB) en **clases parciales por entidad**: `AdminController.cs` (base, dashboard, subida de imágenes), `AdminController.Products.cs`, `.Services.cs`, `.Guides.cs`, `.Transports.cs`, `.Reservations.cs`. Misma clase y mismas rutas; solo se separan archivos.
- [ ] Crear `GUIA_DE_ESTUDIO.md` (máximo 2 páginas, en español):
  - Qué hace cada carpeta.
  - Diagrama de texto de las entidades y sus relaciones.
  - Flujo de una petición: ruta → controlador → vista, con un ejemplo real (catálogo de productos).
  - Cómo funciona el login (Identity, roles, Google/GitHub).
  - Cómo correr el proyecto y cómo se despliega en Render, con **nombres** de variables de entorno, nunca valores.
- [ ] Actualizar `Agente/.agent/PROJECT_STATE.md`, `TASKS.md` y `MEMORY.md` con el estado real: 7 migraciones, sin AJAX, un solo layout, etc. Corregir "Argentina" → Bolivia. No incluyas correos ni credenciales en esos `.md`.
- [ ] Revisión final: `dotnet build` con 0 errores, `git status` limpio, y un resumen de todo lo que quedó pendiente.
