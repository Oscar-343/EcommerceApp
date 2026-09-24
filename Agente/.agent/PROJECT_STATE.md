# Estado del proyecto

> Actualizado al terminar la Fase 7 de `Agente/PLAN_REPORTES.md` (carrito, pedidos, reservas reales y reportes) y las Fases 2 a 7 de `Agente/PLAN_HOME_MAPA_Y_CORRECCIONES.md` (rama `home-mapa`; la Fase 1, hero con video fijo, sigue pendiente). Sin correos ni credenciales.

## Stack

ASP.NET Core MVC (net10.0), EF Core + PostgreSQL (Supabase), Identity (roles `Admin`/`User`, login social Google/GitHub opcional), Bootstrap/jQuery. Un solo proyecto (`EcommerceApp.csproj`). Desplegado en Render.

## Base de datos

10 migraciones creadas, en orden: `InitialCreate`, `AddBusinessEntities`, `AddProductPromotion`, `AddProductFields`, `ExtendServiceModel`, `AddCartAndFavorites`, `AddCartAndFavorites2` (vacía, no se borró), `AddProductGalleryAndServiceProducts`, `AddOrdersAndOrderItems`, `AddReservationRealBooking`.

Entidades: `Product`, `Service` (con `Guide`/`Transport` opcionales), `ServiceProduct` (tabla puente Service↔Product, "equipamiento recomendado"), `Reservation` (PK propia `Id`, `TripDate` de salida, índice único `UserId`+`ServiceId`+`TripDate`), `Order`/`OrderItem` (pedido y sus líneas, snapshot de producto), `CartItem`/`FavoriteItem` (sin FK real, se limpian a mano al borrar Product/Service), `Guide`, `Transport`, `ApplicationUser` (Identity).

## Frontend

- Un solo `_Layout.cshtml` para toda la parte pública (navbar + footer compartidos en `_Navbar.cshtml`), `_AdminLayout.cshtml` para el panel admin, `_AuthLayout.cshtml` para login/registro.
- "Agregar al carrito" y "Favoritos" son POST con token antiforgery (antes usaban `localStorage`/`console.log` simulados). `CartController.Add` y `FavoritesController.Toggle` responden JSON cuando la petición es AJAX (`X-Requested-With: XMLHttpRequest`) y redirigen en caso contrario.
- Home sin la sección "EXPLORA A TU MANERA": la navegación por categorías solo está en Rutas (`Services/Index`).
- Mapa de rutas en el Home: un marcador por ruta activa con coordenadas de inicio, sin límite de cantidad. Encuadre inicial en Bolivia (`data-initial-view="bolivia"`); las rutas de otros países aparecen al alejar el zoom. `wwwroot/js/rutas-map.js` es compartido por Home, `Services/Index` y `Services/Details`; el popup se arma con `textContent` (ningún texto se interpreta como HTML).
- Paleta de colores centralizada en `wwwroot/css/tokens.css`.
- Región del negocio: Cochabamba, **Bolivia** (no Argentina).

## Backend / controladores

- `AdminController` dividido en clases parciales por entidad: `AdminController.cs` (constructor, dashboard, subida de imágenes, helpers compartidos), `.Products.cs`, `.Services.cs`, `.Guides.cs`, `.Transports.cs`, `.Reservations.cs`, `.Orders.cs`, `.Reports.cs`. Mismas rutas y lógica que antes de dividir.
- `OrdersController` y `ReservationsController` (`[Authorize]`) son los controladores públicos para checkout y reservas del cliente.
- `Services/ReportService.cs` (`AddScoped`) centraliza las consultas de los 7 reportes del panel admin; sus ViewModels viven en `Models/Reports/`.
- Categorías de productos y rutas son las 10/5 reales que usan los seeders (no listas inventadas).
- Coordenadas de las rutas (`StartLatitude`/`StartLongitude`/`EndLatitude`/`EndLongitude`) editables desde `ServiceCreate`/`ServiceEdit`, con `[Range]`. Los `input type="number"` se leen con punto decimal aunque el servidor esté en `es-BO` (ASP.NET agrega el campo oculto `__Invariant`), por eso no se cambió la cultura.
- El formulario de rutas acepta **enlaces de Google Maps** (largos, cortos de "Compartir" `maps.app.goo.gl` o coordenadas pegadas) para llenar solas la latitud y longitud: `wwwroot/js/admin-map-link.js` llama a `Admin/ResolveMapLink`, que usa `Services/MapLinkService.cs` (`AddHttpClient`, sin redirección automática: solo sigue enlaces de dominios de Google). El enlace **no se guarda**; en la base siguen solo latitud y longitud (sin migración ni cambios en `Service`).
- `Product.Name` y `Service.Name` sin `RegularExpression` (en JavaScript `\w` no acepta acentos). `Guide.Name` y `Transport.Name` todavía lo tienen.
- Productos y rutas validan el formulario antes de subir imágenes a Supabase (no quedan archivos huérfanos si hay errores).
- Operaciones simultáneas: el checkout descuenta stock con un update atómico (`ExecuteUpdate` con `Stock >= cantidad`) dentro de la transacción; cancelar un pedido (cliente o admin) devuelve el stock también de forma atómica; crear una reserva bloquea la ruta con `SELECT ... FOR UPDATE` antes de calcular el cupo.
- `Edit` de productos, rutas, guías y transportes no pisa `CreatedAt` (`IsModified = false`).
- Moneda única formateada con `Helpers/PriceFormatter.cs` (`"Bs. " + N2`). CSV de reportes formateado con `Helpers/CsvHelper.cs`.

## Pendientes conocidos

Ver `Agente/.agent/TASKS.md` (bug de `CreatedAt` pisado en `GuideEdit`/`TransportEdit`; gráficos con Chart.js en reportes, omitidos por decisión del usuario).
