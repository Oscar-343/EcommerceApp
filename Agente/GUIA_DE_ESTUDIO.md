# Guía de estudio — Tren al Sur

## Qué hace cada carpeta

- **`Controllers/`**: un controlador MVC por sección (`HomeController`, `ProductsController`, `ServicesController`, `CartController`, `OrdersController`, `ReservationsController`, `FavoritesController`, `AccountController`, `AdminController` dividido en clases parciales: `AdminController.cs`, `.Products.cs`, `.Services.cs`, `.Guides.cs`, `.Transports.cs`, `.Reservations.cs`, `.Orders.cs`, `.Reports.cs`).
- **`Models/`**: entidades de EF Core (`Product`, `Service`, `Guide`, `Transport`, `Reservation`, `Order`, `OrderItem`, `CartItem`, `FavoriteItem`, `ServiceProduct`, `ApplicationUser`) y ViewModels de página (`HomeViewModel`, `ProductosViewModel`, `ServiciosViewModel`, `CatalogViewModel`, etc.). `Models/Reports/` tiene un ViewModel por reporte (`IncomeReportViewModel`, `ReservationsReportViewModel`, `ProductSalesReportViewModel`, `InventoryReportViewModel`, `DemandReportViewModel`, `UsersReportViewModel`, `GuidesTransportReportViewModel`).
- **`Views/`**: una carpeta por controlador con sus `.cshtml` (incluye `Views/Orders/`, `Views/Reservations/` y las vistas de reportes en `Views/Admin/`, junto al resto del panel), más `Views/Shared/` para `_Layout.cshtml` (parte pública), `_AdminLayout.cshtml` (panel admin), `_AuthLayout.cshtml` (login/registro) y parciales reutilizables (`_Navbar`, `_ProductoCard`, etc.).
- **`Data/`**: `ApplicationDbContext` (mapeo EF Core) y los *seeders* (`ProductSeeder`, `ServiceSeeder`, `AdminSeeder`) que cargan datos iniciales al arrancar.
- **`Services/`**: `IImageStorageService`/`SupabaseImageStorageService` (sube archivos a Supabase Storage), `IEmailSender`/`SmtpEmailSender` (correo de recuperación de contraseña) y `ReportService` (`AddScoped`, calcula los 7 reportes del panel admin a partir de EF Core).
- **`Helpers/`**: `PriceFormatter` (formatea precios como `"Bs. " + N2` en un solo lugar) y `CsvHelper` (arma el CSV de cada reporte).
- **`Migrations/`**: historial de cambios de esquema generado por EF Core (no se editan a mano).
- **`wwwroot/`**: CSS, JS e imágenes estáticas servidas directamente.

## Entidades y relaciones

```
ApplicationUser (Identity)
   |
   |  1---N (sin FK real, se limpia a mano al borrar)
   v
CartItem, FavoriteItem  (Type: "Product" | "Service", ItemId)

ApplicationUser 1---N Order 1---N OrderItem N---1 Product (opcional, SetNull)
   Order: Id, Status (Pendiente|Entregado|Cancelado), Total, DeliveredAt.
   OrderItem es una "foto" del producto al momento de comprar
   (ProductName, ProductCategory, UnitPrice quedan guardados aunque
   el producto original se edite o se borre después).

ApplicationUser 1---N Reservation N---1 Service
   PK propia Id (ya no compuesta). Índice único (UserId, ServiceId, TripDate):
   el mismo usuario puede reservar la misma ruta varias veces, pero no dos
   veces la misma fecha de salida (TripDate, distinta de BookingDate).
   Status: Pendiente|Recorrido|Acabado|Cancelado. CompletedAt se llena
   al pasar a Acabado.

Service N---1 Guide     (GuideId opcional)
Service N---1 Transport (TransportId opcional)

Service N---N Product  vía ServiceProduct (PK compuesta ServiceId+ProductId)
   "Equipamiento recomendado" de una ruta. Cascade: si se borra la ruta
   o el producto, el vínculo desaparece solo.
```

`CartItem` y `FavoriteItem` no tienen clave foránea real hacia `Product`/`Service` (guardan `Type` + `ItemId` a mano), así que al borrar un producto o ruta en `AdminController` hay que borrar también sus filas en `FavoriteItems`/`CartItems` explícitamente — no lo hace la base de datos sola. `OrderItem.ProductId` sí es una FK real, pero nullable con `DeleteBehavior.SetNull`: borrar un producto no rompe el historial de pedidos, solo desconecta la fila (el snapshot de nombre/categoría/precio sigue mostrándose).

## Flujo de una petición (ejemplo: catálogo de productos)

1. El navegador pide `GET /Products?category=Mochilas`.
2. Enrutamiento por convención (`Program.cs`: `{controller=Home}/{action=Index}/{id?}`) lo manda a `ProductsController.Index`.
3. El controlador arma una consulta LINQ sobre `context.Products` (filtros de categoría/oferta/búsqueda) y la pasa a la vista dentro de un `ProductosViewModel`.
4. `Views/Products/Index.cshtml` usa `_Layout.cshtml` (navbar/footer compartidos), recorre la lista y renderiza el parcial `_ProductoCard.cshtml` por cada producto.
5. El botón "Agregar" hace `POST /Cart/Add` con el token antiforgery; `CartController.Add` valida stock/cantidad y guarda un `CartItem`.

El resto de páginas públicas (`Home`, `Services`, `Cart`, `Favorites`) siguen el mismo patrón: ruta → controlador → consulta EF Core → vista con `_Layout`.

## Flujo carrito → pedido

1. `Views/Cart/Index.cshtml` tiene un `<form asp-controller="Orders" asp-action="Checkout">` con el botón "Confirmar pedido" (deshabilitado si el carrito está vacío o algún ítem quedó sin stock suficiente).
2. `OrdersController.Checkout` (`[Authorize]`) abre una transacción, revalida el stock de cada `CartItem` del usuario, crea un `Order` y un `OrderItem` por línea (snapshot de `ProductName`/`ProductCategory`/`UnitPrice` con el precio vigente), descuenta el stock de cada `Product`, calcula `Order.Total` en el servidor y vacía el carrito.
3. `Views/Orders/Index.cshtml` ("Mis pedidos") y `Details.cshtml` muestran los pedidos propios. `Cancel` solo funciona si el pedido sigue `Pendiente` y devuelve el stock.
4. En el panel admin, `AdminController.Orders.cs` lista y filtra pedidos por estado; `OrderUpdateStatus` solo permite `Pendiente → Entregado` (guarda `DeliveredAt = UtcNow`) o `Pendiente → Cancelado` (devuelve stock). Ambos son estados finales.

## Flujo de reserva

1. `Views/Services/Details.cshtml` tiene un formulario (fecha de salida + número de personas) para usuarios con sesión; muestra el total estimado antes de enviar.
2. `ReservationsController.Create` (`[Authorize]`) valida que la ruta esté activa, que `TripDate` sea desde mañana (hora Bolivia, UTC−4), que `PeopleCount` esté entre 1 y `Service.MaxGroupSize`, y que sumando las personas de reservas no canceladas de esa ruta y esa fecha no se supere `MaxGroupSize`. También rechaza una segunda reserva del mismo usuario/ruta/fecha (índice único). El servidor calcula `UnitPrice`/`TotalPrice`.
3. `Views/Reservations/Index.cshtml` ("Mis reservas") lista las propias; `Cancel` solo si están `Pendiente`.
4. En admin, `AdminController.Reservations.cs` aplica las transiciones válidas (`Pendiente → Recorrido → Acabado`; `Pendiente` o `Recorrido → Cancelado`) y guarda `CompletedAt` al llegar a `Acabado`. Una reserva `Acabado` no se puede borrar (ya representa un ingreso confirmado); `Pendiente` y `Cancelado` sí.

## Regla de ingresos y reportes

El dinero solo se cuenta como ingreso cuando termina el ciclo: una reserva en `Acabado` (fecha `CompletedAt`) o un pedido en `Entregado` (fecha `DeliveredAt`). Todo lo demás que no esté `Cancelado` se muestra aparte como **"por confirmar"**, nunca sumado al ingreso. Para agrupar por día/mes con hora de Bolivia, el código resta 4 horas a los timestamps UTC con una constante (`Services/ReportService.cs`), sin depender de la configuración de zona horaria del servidor.

`Services/ReportService.cs` (`AddScoped`) tiene un método por reporte, todos con `AsNoTracking()`, que arma el panel **Reportes** del admin (`AdminController.Reports.cs`, con una acción de vista y una `...Csv` de exportación por reporte):

- `GetIncomeReportAsync`: ingresos por reservas y por pedidos, total general, tabla por mes y el bloque "por confirmar".
- `GetReservationsReportAsync`: reservas filtradas por fecha de salida/estado/ruta, tasa de cancelación, tablas por ruta y por mes.
- `GetProductSalesReportAsync`: ventas de `OrderItem` de pedidos `Entregado`, por producto y por categoría.
- `GetInventoryReportAsync`: stock bajo/agotado, valor de inventario, productos por categoría/marca y en oferta (sin filtro de fecha).
- `GetDemandReportAsync`: productos/rutas más guardados en favoritos y productos más presentes en carritos activos (sin filtro de fecha).
- `GetUsersReportAsync`: registros de usuarios por mes.
- `GetGuidesTransportReportAsync`: rutas por guía/transporte y reservas `Acabado` por guía.

**Exportar e imprimir:** cada reporte tiene un botón de CSV (`Helpers/CsvHelper.cs`: separador `;`, UTF-8 con BOM para que Excel en español lo abra bien, y escapa con `'` las celdas que empiezan con `=`, `+`, `-` o `@` para evitar inyección de fórmulas) y un botón "Imprimir / Guardar como PDF" que usa el bloque `@media print` de `wwwroot/css/admin.css` (oculta sidebar/topbar/botones al imprimir; no hay una vista ni librería de PDF aparte).

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
