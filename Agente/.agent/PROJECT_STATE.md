# Estado del proyecto

> Actualizado al terminar la Fase 7 del refactor (`Agente/PLAN_REFACTOR.md`). Sin correos ni credenciales.

## Stack

ASP.NET Core MVC (net10.0), EF Core + PostgreSQL (Supabase), Identity (roles `Admin`/`User`, login social Google/GitHub opcional), Bootstrap/jQuery. Un solo proyecto (`EcommerceApp.csproj`). Desplegado en Render.

## Base de datos

8 migraciones aplicadas, en orden: `InitialCreate`, `AddBusinessEntities`, `AddProductPromotion`, `AddProductFields`, `ExtendServiceModel`, `AddCartAndFavorites`, `AddCartAndFavorites2` (vacía, no se borró), `AddProductGalleryAndServiceProducts`.

Entidades: `Product`, `Service` (con `Guide`/`Transport` opcionales), `ServiceProduct` (tabla puente Service↔Product, "equipamiento recomendado"), `Reservation` (PK compuesta `UserId`+`ServiceId`), `CartItem`/`FavoriteItem` (sin FK real, se limpian a mano al borrar Product/Service), `Guide`, `Transport`, `ApplicationUser` (Identity).

## Frontend

- Un solo `_Layout.cshtml` para toda la parte pública (navbar + footer compartidos en `_Navbar.cshtml`), `_AdminLayout.cshtml` para el panel admin, `_AuthLayout.cshtml` para login/registro.
- Sin AJAX en el catálogo: "Agregar al carrito" y "Favoritos" son POST normales con token antiforgery (antes usaban `localStorage`/`console.log` simulados).
- Paleta de colores centralizada en `wwwroot/css/tokens.css`.
- Región del negocio: Cochabamba, **Bolivia** (no Argentina).

## Backend / controladores

- `AdminController` dividido en clases parciales por entidad: `AdminController.cs` (constructor, dashboard, subida de imágenes, helpers compartidos), `.Products.cs`, `.Services.cs`, `.Guides.cs`, `.Transports.cs`, `.Reservations.cs`. Mismas rutas y lógica que antes de dividir.
- Categorías de productos y rutas son las 10/5 reales que usan los seeders (no listas inventadas).
- Moneda única formateada con `Helpers/PriceFormatter.cs` (`"Bs. " + N2`).

## Pendientes conocidos

Ver `Agente/.agent/TASKS.md` (reservas: PK compuesta sin fecha de salida, no soporta múltiples reservas por usuario/ruta).
