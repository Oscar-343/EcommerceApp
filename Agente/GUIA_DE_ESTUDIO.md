# Guía de estudio — Tren al Sur

## Qué hace cada carpeta

- **`Controllers/`**: un controlador MVC por sección (`HomeController`, `ProductsController`, `ServicesController`, `CartController`, `FavoritesController`, `AccountController`, `AdminController` dividido en clases parciales: `AdminController.cs`, `.Products.cs`, `.Services.cs`, `.Guides.cs`, `.Transports.cs`, `.Reservations.cs`).
- **`Models/`**: entidades de EF Core (`Product`, `Service`, `Guide`, `Transport`, `Reservation`, `CartItem`, `FavoriteItem`, `ServiceProduct`, `ApplicationUser`) y ViewModels de página (`HomeViewModel`, `ProductosViewModel`, `ServiciosViewModel`, `CatalogViewModel`, etc.).
- **`Views/`**: una carpeta por controlador con sus `.cshtml`, más `Views/Shared/` para `_Layout.cshtml` (parte pública), `_AdminLayout.cshtml` (panel admin), `_AuthLayout.cshtml` (login/registro) y parciales reutilizables (`_Navbar`, `_ProductoCard`, etc.).
- **`Data/`**: `ApplicationDbContext` (mapeo EF Core) y los *seeders* (`ProductSeeder`, `ServiceSeeder`, `AdminSeeder`) que cargan datos iniciales al arrancar.
- **`Services/`**: `IImageStorageService`/`SupabaseImageStorageService` (sube archivos a Supabase Storage) e `IEmailSender`/`SmtpEmailSender` (correo de recuperación de contraseña).
- **`Helpers/`**: `PriceFormatter`, formatea precios como `"Bs. " + N2` en un solo lugar.
- **`Migrations/`**: historial de cambios de esquema generado por EF Core (no se editan a mano).
- **`wwwroot/`**: CSS, JS e imágenes estáticas servidas directamente.

## Entidades y relaciones

```
ApplicationUser (Identity)
   |
   |  1---N (sin FK real, se limpia a mano al borrar)
   v
CartItem, FavoriteItem  (Type: "Product" | "Service", ItemId)

ApplicationUser 1---N Reservation N---1 Service
   PK compuesta (UserId, ServiceId): una reserva activa por usuario y ruta.

Service N---1 Guide     (GuideId opcional)
Service N---1 Transport (TransportId opcional)

Service N---N Product  vía ServiceProduct (PK compuesta ServiceId+ProductId)
   "Equipamiento recomendado" de una ruta. Cascade: si se borra la ruta
   o el producto, el vínculo desaparece solo.
```

`CartItem` y `FavoriteItem` no tienen clave foránea real hacia `Product`/`Service` (guardan `Type` + `ItemId` a mano), así que al borrar un producto o ruta en `AdminController` hay que borrar también sus filas en `FavoriteItems`/`CartItems` explícitamente — no lo hace la base de datos sola.

## Flujo de una petición (ejemplo: catálogo de productos)

1. El navegador pide `GET /Products?category=Mochilas`.
2. Enrutamiento por convención (`Program.cs`: `{controller=Home}/{action=Index}/{id?}`) lo manda a `ProductsController.Index`.
3. El controlador arma una consulta LINQ sobre `context.Products` (filtros de categoría/oferta/búsqueda) y la pasa a la vista dentro de un `ProductosViewModel`.
4. `Views/Products/Index.cshtml` usa `_Layout.cshtml` (navbar/footer compartidos), recorre la lista y renderiza el parcial `_ProductoCard.cshtml` por cada producto.
5. El botón "Agregar" hace `POST /Cart/Add` con el token antiforgery; `CartController.Add` valida stock/cantidad y guarda un `CartItem`.

El resto de páginas públicas (`Home`, `Services`, `Cart`, `Favorites`) siguen el mismo patrón: ruta → controlador → consulta EF Core → vista con `_Layout`.

## Login (ASP.NET Identity)

- `Program.cs` registra Identity (`AddIdentity<ApplicationUser, IdentityRole>`) y siembra los roles `Admin`/`User` al arrancar si no existen.
- `AdminSeeder` crea (o promueve a Admin) la cuenta definida por configuración (`Admin:Email`/`Admin:Password`), nunca hardcodeada.
- Login social: Google y GitHub se registran en `Program.cs` **solo si** hay `ClientId`/`ClientSecret` configurados; si faltan, la app sigue funcionando sin esos botones.
- `AccountController.Login` respeta `returnUrl` (validado con `Url.IsLocalUrl`) y usa `lockoutOnFailure: true`.
- `[Authorize(Roles = "Admin")]` en `AdminController` protege todo el panel.

## Cómo correr el proyecto

```
dotnet run --launch-profile http
```

Corre en `http://localhost:5187` (puerto fijo por los callbacks OAuth de Google/GitHub). `appsettings.Example.json` documenta todas las claves necesarias (`ConnectionStrings`, `Authentication`, `Admin`, SMTP, Supabase) con valores vacíos o de referencia; copialo como base para tu `appsettings.Development.json` local (no se versiona).

## Despliegue en Render

Variables de entorno a definir en el servicio de Render (solo nombres, nunca valores acá):

- `ConnectionStrings__postgresql` — cadena de conexión a Supabase/PostgreSQL.
- `Authentication__Google__ClientId`, `Authentication__Google__ClientSecret`.
- `Authentication__GitHub__ClientId`, `Authentication__GitHub__ClientSecret`.
- `Admin__Email`, `Admin__Password` — cuenta de administración inicial.
- `Email__Host`, `Email__Port`, `Email__Username`, `Email__Password`, `Email__From` — envío de correo (recuperación de contraseña).
- `Supabase__Url`, `Supabase__ServiceRoleKey`, `Supabase__Bucket` — subida de imágenes/video desde el panel admin.

`Program.cs` agrega `UseForwardedHeaders` antes de `UseHttpsRedirection` porque el proxy de Render no es loopback; sin eso, las URLs generadas (login social, "olvidé mi contraseña") saldrían con `http` en vez de `https`.
