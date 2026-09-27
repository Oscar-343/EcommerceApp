# Guía de estudio — Tren al Sur

## Qué hace cada carpeta

- **`Controllers/`**: un controlador MVC por sección (`HomeController`, `ProductsController`, `ServicesController`, `CartController`, `CheckoutController`, `OrdersController`, `ReservationsController`, `FavoritesController`, `AccountController`, `AdminController` dividido en clases parciales: `AdminController.cs`, `.Products.cs`, `.Services.cs`, `.Guides.cs`, `.Transports.cs`, `.Reservations.cs`, `.Orders.cs`, `.Reports.cs`).
- **`Models/`**: entidades de EF Core (`Product`, `Service`, `Guide`, `Transport`, `Reservation`, `Order`, `OrderItem`, `CartItem`, `FavoriteItem`, `ServiceProduct`, `ApplicationUser`) y ViewModels de página (`HomeViewModel`, `ProductosViewModel`, `ServiciosViewModel`, `CatalogViewModel`, `CheckoutViewModel`, `ResumenCompraViewModel`, etc.). `Models/Reports/` tiene un ViewModel por reporte (`IncomeReportViewModel`, `ReservationsReportViewModel`, `ProductSalesReportViewModel`, `InventoryReportViewModel`, `DemandReportViewModel`, `UsersReportViewModel`, `GuidesTransportReportViewModel`).
- **`Views/`**: una carpeta por controlador con sus `.cshtml` (incluye `Views/Orders/`, `Views/Reservations/` y las vistas de reportes en `Views/Admin/`, junto al resto del panel), más `Views/Shared/` para `_Layout.cshtml` (parte pública), `_AdminLayout.cshtml` (panel admin), `_AuthLayout.cshtml` (login/registro) y parciales reutilizables (`_Navbar`, `_ProductoCard`, `_Aviso` —el mensaje emergente de `TempData["Success"]`—, etc.).
- **`Data/`**: `ApplicationDbContext` (mapeo EF Core) y los *seeders* (`ProductSeeder`, `ServiceSeeder`, `AdminSeeder`) que cargan datos iniciales al arrancar.
- **`Services/`**: `IImageStorageService`/`SupabaseImageStorageService` (sube archivos a Supabase Storage), `IEmailSender`/`SmtpEmailSender` (correo de recuperación de contraseña), `PedidoService` (convierte el carrito en pedido; lo usan `OrdersController` y `CheckoutController`), `IPagoService`/`PagoSimuladoService` (pago simulado del checkout) y `ReportService` (`AddScoped`, calcula los 7 reportes del panel admin a partir de EF Core).
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
   Si el pedido vino del checkout, también guarda los datos de entrega
   (NombreEntrega, TelefonoEntrega, EmailEntrega, CiudadEntrega,
   DireccionEntrega, ReferenciaEntrega) y del pago simulado (MetodoPago,
   TarjetaMarca, TarjetaUltimos4, CodigoTransaccion). Todos son opcionales.

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

1. `Views/Cart/Index.cshtml` tiene el botón "Continuar con la compra" que lleva al **checkout** (`/Checkout`); está deshabilitado si el carrito está vacío o algún ítem quedó sin stock suficiente. El checkout se explica en la sección siguiente.
2. La creación del pedido está en `PedidoService.CrearDesdeCarritoAsync` (antes estaba dentro de `OrdersController.Checkout`, que sigue existiendo y ahora solo llama al servicio): abre una transacción, revalida el stock de cada `CartItem` del usuario, descuenta el stock con una sola operación en la BD por producto, crea un `Order` en estado `Pendiente` y un `OrderItem` por línea (snapshot de `ProductName`/`ProductCategory`/`UnitPrice` con el precio vigente), calcula `Order.Total` en el servidor y vacía el carrito.
3. `Views/Orders/Index.cshtml` ("Mis pedidos") y `Details.cshtml` muestran los pedidos propios. `Cancel` solo funciona si el pedido sigue `Pendiente` y devuelve el stock.
4. En el panel admin, `AdminController.Orders.cs` lista y filtra pedidos por estado; `OrderUpdateStatus` solo permite `Pendiente → Entregado` (guarda `DeliveredAt = UtcNow`) o `Pendiente → Cancelado` (devuelve stock). Ambos son estados finales.

## Checkout y pago simulado

El checkout es la pantalla de compra: datos de entrega → método de pago → confirmación. **El pago es simulado**: no hay pasarela real, no se cobra dinero y ningún dato de tarjeta sale del navegador. La interfaz se comporta como una tienda real, pero por dentro el servidor solo "imita" la respuesta de una pasarela.

### El flujo completo

```
Carrito ──► Checkout ──► Pago simulado ──► Pedido ──► Confirmación
/Cart       GET /Checkout   IPagoService     PedidoService   GET /Checkout/Confirmacion/{id}
            POST /Checkout/Pagar
```

1. **Carrito** (`/Cart`): el botón "Continuar con la compra" lleva a `GET /Checkout`.
2. **Checkout** (`CheckoutController.Index`, `[Authorize]`): lee el carrito con `PedidoService.ObtenerCarritoAsync`. Si está vacío o falta stock, vuelve al carrito con un aviso. Si no, arma el resumen con precios de la base de datos (`PedidoService.ArmarResumen`) y prellena nombre, email, teléfono y dirección del usuario.
3. **En la página** (`Views/Checkout/Index.cshtml` + `wwwroot/js/checkout.js`), sin recargar:
   - paso "Destino": los datos de entrega se validan en el navegador (jQuery Validation, con las reglas del `CheckoutViewModel`);
   - paso "Pago": se elige Tarjeta, QR o Transferencia. El botón "Pagar" solo se habilita cuando el método está completo (tarjeta válida o la casilla "Ya realicé el pago" marcada).
4. **Pago simulado** (`POST /Checkout/Pagar`):
   - el servidor **vuelve a leer el carrito y recalcula el total**;
   - valida los datos de entrega;
   - limpia `TarjetaMarca`/`TarjetaUltimos4`;
   - llama a `IPagoService.ProcesarAsync(metodo, total)`. `PagoSimuladoService` espera 1,2 segundos (como una pasarela) y devuelve un código tipo `TAS-20260927-1877`.
5. **Pedido**: `PedidoService.CrearDesdeCarritoAsync` crea el pedido con el **mismo flujo que ya existía** (descuenta stock, guarda las líneas y vacía el carrito) y además guarda los datos de entrega, el método de pago y el código. El pedido nace **`Pendiente`**, aunque el pago simulado se haya "aprobado": según la regla de ingresos, solo cuenta como dinero cuando el admin lo marca `Entregado`.
6. **Confirmación** (`CheckoutController.Confirmacion`): muestra el pedido **solo si es del usuario actual** (si no, 404), con su estado real.

### Por qué existe `IPagoService`

`CheckoutController` no conoce la clase `PagoSimuladoService`: solo pide "algo que implemente `IPagoService`". Quien decide cuál se usa es `Program.cs`:

```csharp
builder.Services.AddScoped<IPagoService, PagoSimuladoService>();
```

Si algún día se integra una pasarela real, se crea otra clase (por ejemplo `PagoStripeService : IPagoService`) y se cambia **solo esa línea**. El controlador, las vistas y el pedido no se tocan. Esto se llama **inyección de dependencias**: el controlador depende de un "contrato" (la interfaz), no de una clase concreta.

### Por qué los datos de la tarjeta nunca llegan al servidor

Un formulario HTML solo envía los campos que tienen atributo **`name`**. Los cuatro inputs de la tarjeta (número, titular, vencimiento y CVV, en `_PasoPago.cshtml`) **no tienen `name`**: el navegador los usa para validar y para la vista previa animada, pero nunca los incluye en el POST. Además:

- tienen `autocomplete="off"` (el navegador no los guarda) y no tienen el micrófono del dictado por voz, que manda el audio a un servicio externo;
- al pagar, `checkout.js` llena solo dos campos ocultos: `TarjetaMarca` ("VISA") y `TarjetaUltimos4` ("4242"), lo mínimo para mostrar "VISA •••• 4242";
- el servidor no confía ni en esos dos: `CheckoutController.Pagar` solo acepta una marca conocida y exactamente 4 dígitos, y todo lo demás lo descarta;
- `checkout.js` nunca escribe nada en `console.log`.

Se puede comprobar en DevTools → Network → la petición `Pagar` → Payload: no aparece el número completo, ni la fecha ni el CVV.

Por la misma idea, el **total nunca viene del formulario**: `CheckoutViewModel.Resumen` tiene `[BindNever]`, así que aunque alguien envíe `Resumen.Total=1`, el servidor lo ignora y calcula el total con los precios de la base de datos.

### Qué hace el algoritmo de Luhn

Es una cuenta rápida para detectar **errores de tipeo** en números de tarjeta: un dígito equivocado o dos dígitos invertidos. No dice si la tarjeta existe ni si tiene saldo; solo si el número "tiene forma" de tarjeta válida. Está en `luhnValido()`, dentro de `checkout.js`:

1. Se recorre el número de **derecha a izquierda**.
2. Uno de cada dos dígitos (el 2.º, el 4.º, el 6.º…) se **multiplica por 2**. Si el resultado es mayor que 9, se le **resta 9**.
3. Se **suman** todos los dígitos, los cambiados y los que no.
4. Si la suma termina en **0** (es múltiplo de 10), el número es válido.

Por eso `4242 4242 4242 4242` pasa (suma 80) y `1234 5678 9012 3456` no (suma 64). Además de Luhn, la marca se detecta por el comienzo del número: `4` es VISA; `51–55` o `2221–2720` es MASTERCARD.

### Qué es el patrón Post-Redirect-Get (PRG)

Si una página que se cargó con un **POST** se recarga, el navegador vuelve a enviar ese POST. En una compra, eso crearía **otro pedido**.

PRG lo evita en tres pasos:

1. **Post**: el formulario envía `POST /Checkout/Pagar`.
2. **Redirect**: al terminar, el servidor no devuelve una página. Responde "andá a `/Checkout/Confirmacion/5`" (`RedirectToAction`).
3. **Get**: el navegador pide esa página con un **GET**.

Ahora la página que el usuario ve vino de un GET, que solo *lee* el pedido. Recargarla, o volver con "Atrás", no crea nada nuevo. Solo cuando hay un error (datos de entrega inválidos, pago rechazado), `Pagar` devuelve la vista directamente, porque en ese caso no se creó ningún pedido.

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
