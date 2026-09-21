# Estado del proyecto

> Actualizado al terminar la Fase 7 de `Agente/PLAN_REPORTES.md` (carrito, pedidos, reservas reales y reportes). Sin correos ni credenciales.

## Stack

ASP.NET Core MVC (net10.0), EF Core + PostgreSQL (Supabase), Identity (roles `Admin`/`User`, login social Google/GitHub opcional), Bootstrap/jQuery. Un solo proyecto (`EcommerceApp.csproj`). Desplegado en Render.

## Base de datos

10 migraciones creadas, en orden: `InitialCreate`, `AddBusinessEntities`, `AddProductPromotion`, `AddProductFields`, `ExtendServiceModel`, `AddCartAndFavorites`, `AddCartAndFavorites2` (vacía, no se borró), `AddProductGalleryAndServiceProducts`, `AddOrdersAndOrderItems`, `AddReservationRealBooking`.

Entidades: `Product`, `Service` (con `Guide`/`Transport` opcionales), `ServiceProduct` (tabla puente Service↔Product, "equipamiento recomendado"), `Reservation` (PK propia `Id`, `TripDate` de salida, índice único `UserId`+`ServiceId`+`TripDate`), `Order`/`OrderItem` (pedido y sus líneas, snapshot de producto), `CartItem`/`FavoriteItem` (sin FK real, se limpian a mano al borrar Product/Service), `Guide`, `Transport`, `ApplicationUser` (Identity).

## Frontend

- Un solo `_Layout.cshtml` para toda la parte pública (navbar + footer compartidos en `_Navbar.cshtml`), `_AdminLayout.cshtml` para el panel admin, `_AuthLayout.cshtml` para login/registro.
- Sin AJAX en el catálogo: "Agregar al carrito" y "Favoritos" son POST normales con token antiforgery (antes usaban `localStorage`/`console.log` simulados).
- Paleta de colores centralizada en `wwwroot/css/tokens.css`.
- Región del negocio: Cochabamba, **Bolivia** (no Argentina).

## Backend / controladores

- `AdminController` dividido en clases parciales por entidad: `AdminController.cs` (constructor, dashboard, subida de imágenes, helpers compartidos), `.Products.cs`, `.Services.cs`, `.Guides.cs`, `.Transports.cs`, `.Reservations.cs`, `.Orders.cs`, `.Reports.cs`. Mismas rutas y lógica que antes de dividir.
- `OrdersController` y `ReservationsController` (`[Authorize]`) son los controladores públicos para checkout y reservas del cliente.
- `Services/ReportService.cs` (`AddScoped`) centraliza las consultas de los 7 reportes del panel admin; sus ViewModels viven en `Models/Reports/`.
- Categorías de productos y rutas son las 10/5 reales que usan los seeders (no listas inventadas).
- Moneda única formateada con `Helpers/PriceFormatter.cs` (`"Bs. " + N2`). CSV de reportes formateado con `Helpers/CsvHelper.cs`.

## Pendientes conocidos

Ver `Agente/.agent/TASKS.md` (bug de `CreatedAt` pisado en `GuideEdit`/`TransportEdit`; gráficos con Chart.js en reportes, omitidos por decisión del usuario).
