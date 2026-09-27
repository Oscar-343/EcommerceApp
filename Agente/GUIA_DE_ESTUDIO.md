# Guía de estudio — Tren al Sur

## Qué hace cada carpeta

- **`Controllers/`**: un controlador MVC por sección (`HomeController`, `ProductsController`, `ServicesController`, `CartController`, `CheckoutController`, `OrdersController`, `ReservationsController`, `FavoritesController`, `AccountController`, `AdminController` dividido en clases parciales: `AdminController.cs`, `.Products.cs`, `.Services.cs`, `.Guides.cs`, `.Transports.cs`, `.Reservations.cs`, `.Orders.cs`, `.Reports.cs`), más `ProveedoresController`, `MarcasController` y `AbastecimientosController` del módulo de inventario (también solo Admin).
- **`Models/`**: entidades de EF Core (`Product`, `Service`, `Guide`, `Transport`, `Reservation`, `Order`, `OrderItem`, `CartItem`, `FavoriteItem`, `ServiceProduct`, `ApplicationUser`, y del inventario: `Marca`, `Proveedor`, `ProveedorMarca`, `Abastecimiento`, `DetalleAbastecimiento`) y ViewModels de página (`HomeViewModel`, `ProductosViewModel`, `ServiciosViewModel`, `CatalogViewModel`, `CheckoutViewModel`, `ResumenCompraViewModel`, etc.). `Models/Reports/` tiene un ViewModel por reporte (`IncomeReportViewModel`, `ReservationsReportViewModel`, `ProductSalesReportViewModel`, `InventoryReportViewModel`, `DemandReportViewModel`, `UsersReportViewModel`, `GuidesTransportReportViewModel`). `Models/Inventario/` tiene los formularios y filas del módulo de proveedores (`ProveedorFormViewModel`, `ProveedorListaItem`, `MarcaFormViewModel`, `MarcaListaItem`, `AbastecimientoFormViewModel` + `LineaAbastecimiento`).
- **`Views/`**: una carpeta por controlador con sus `.cshtml` (incluye `Views/Orders/`, `Views/Reservations/` y las vistas de reportes en `Views/Admin/`, junto al resto del panel), más `Views/Shared/` para `_Layout.cshtml` (parte pública), `_AdminLayout.cshtml` (panel admin), `_AuthLayout.cshtml` (login/registro) y parciales reutilizables (`_Navbar`, `_ProductoCard`, `_Aviso` —el mensaje emergente de `TempData["Success"]`—, etc.).
- **`Data/`**: `ApplicationDbContext` (mapeo EF Core) y los *seeders* (`ProductSeeder`, `ServiceSeeder`, `AdminSeeder`) que cargan datos iniciales al arrancar.
- **`Services/`**: `IImageStorageService`/`SupabaseImageStorageService` (sube archivos a Supabase Storage), `IEmailSender`/`SmtpEmailSender` (correo de recuperación de contraseña), `PedidoService` (convierte el carrito en pedido; lo usan `OrdersController` y `CheckoutController`), `IPagoService`/`PagoSimuladoService` (pago simulado del checkout) `ReportService` (`AddScoped`, calcula los 7 reportes del panel admin a partir de EF Core) y, del inventario, `ProveedorService`, `MarcaService` y `AbastecimientoService` (devuelven un `ResultadoOperacion`: el Id creado o el mensaje de error para el usuario).
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

Marca 1---N Product          (MarcaId opcional; reemplazó al texto Product.Brand)
Proveedor N---N Marca        vía ProveedorMarca (PK compuesta ProveedorId+MarcaId)
Proveedor 1---N Abastecimiento 1---N DetalleAbastecimiento N---1 Product
   (explicado en "Proveedores, marcas y abastecimiento")
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

## Proveedores, marcas y abastecimiento

Módulo del panel admin (grupo **Inventario** del menú) para registrar a quién le compra la tienda y **abastecer**: cargar la mercadería que llega, lo que suma stock a los productos. Lo usa solo el administrador; los proveedores no tienen login.

### El modelo de datos

```
Proveedor 1 ─── * ProveedorMarca * ─── 1 Marca 1 ─── * Product
    │
    1
    │
    * Abastecimiento 1 ─── * DetalleAbastecimiento * ─── 1 Product
```

- **`Marca`**: nombre (único), logo opcional y `Activo`. Antes la marca era un texto libre en `Product.Brand`; la migración `AgregarProveedoresYAbastecimientos` creó una fila de `Marcas` por cada texto distinto y enlazó cada producto con su `MarcaId`, y `QuitarBrandDeProductos` borró la columna vieja. Así "Columbia" y "columbia " ya no son dos marcas distintas, y el catálogo filtra por `?marcaId=`.
- **`Proveedor`**: datos de contacto (NIT, teléfono, email…) y `Activo`.
- **`ProveedorMarca`**: qué marcas distribuye cada proveedor (ver "muchos a muchos" abajo).
- **`Abastecimiento`** (la cabecera): qué proveedor entregó, cuándo, el número de comprobante del proveedor, `Total` y `RegistradoPor`.
- **`DetalleAbastecimiento`** (las líneas): producto, `Cantidad`, `CostoUnitario` y `Subtotal`. Guarda también `NombreProducto`: igual que `OrderItem`, si el producto se borra después, `ProductoId` queda en `null` (`SetNull`) pero el historial sigue diciendo qué se compró.
- **`Product.StockMinimo`**: umbral de stock bajo propio de cada producto (antes era un 5 fijo en tres lugares). Con `Stock <= StockMinimo` el producto sale en ocre en el panel y cuenta en la tarjeta "Productos con stock bajo" del resumen.

Ni proveedores ni marcas se borran: se **desactivan**. Un proveedor inactivo no aparece al abastecer, pero su historial sigue visible. Una marca no se puede desactivar mientras tenga productos.

### Qué es una relación muchos a muchos y por qué existe `ProveedorMarca`

Un proveedor distribuye **varias** marcas (Andes Gear trae Osprey y Columbia) y una marca puede venir de **varios** proveedores (Columbia la traen dos distribuidores). Eso es una relación *muchos a muchos* (N a N).

Una tabla relacional no puede guardar "una lista de marcas" dentro de una fila de `Proveedores`. La solución es una **tabla intermedia** donde cada fila es un vínculo:

| ProveedorId | MarcaId |
|---|---|
| 1 (Andes Gear) | 5 (Osprey) |
| 1 (Andes Gear) | 10 (Columbia) |
| 2 (Otro) | 10 (Columbia) |

Su clave primaria es **compuesta** (`ProveedorId` + `MarcaId`, configurada con `HasKey(x => new { x.ProveedorId, x.MarcaId })` en `ApplicationDbContext`): así el mismo vínculo no puede repetirse. Es el mismo patrón que ya usaba `ServiceProduct` (equipamiento recomendado de una ruta).

Para qué sirve en la práctica: al abastecer, el formulario solo ofrece los productos de las marcas del proveedor elegido, y el servidor rechaza un producto de otra marca.

### Qué es una transacción y por qué el abastecimiento la necesita

Registrar un abastecimiento son varias escrituras: la cabecera, una fila de detalle por producto y un aumento de stock por producto. Si el servidor fallara a la mitad (se corta la conexión, un producto se borró justo antes…), podrían quedar **sumados unos stocks sin su abastecimiento**, o un abastecimiento que dice 10 unidades cuando el stock no cambió.

Una **transacción** agrupa todas esas escrituras en una sola unidad: o se guardan **todas** (`CommitAsync`) o **ninguna** (se deshacen si hay un error o si el código llama a `RollbackAsync`). En `AbastecimientoService.RegistrarAsync`:

1. Primero valida todo **antes** de abrir la transacción: el proveedor existe y está activo, hay al menos una línea (y como máximo 100), no hay productos repetidos, cada producto existe y es de una marca del proveedor, y cantidades y costos están en rango.
2. Abre la transacción (`BeginTransactionAsync`).
3. Por cada línea suma el stock con **una sola operación en la base** (`ExecuteUpdateAsync(Stock = Stock + cantidad)`), igual que al cancelar un pedido. Así no pisa una venta que ocurra al mismo tiempo. Si el producto ya no existe, hace `RollbackAsync` y no se guarda nada.
4. Calcula `Subtotal` y `Total` en el servidor, guarda cabecera y detalles (`SaveChangesAsync`) y confirma (`CommitAsync`).

### Por qué los abastecimientos no se editan ni se eliminan

El stock actual de un producto es el resultado de todo lo que entró (abastecimientos) menos todo lo que salió (ventas). Si se pudiera editar un abastecimiento de 10 unidades a 5, o borrarlo, el historial diría una cosa y el stock otra, y ya no se podría explicar de dónde salió cada unidad.

Por eso `AbastecimientosController` no tiene acciones de editar ni borrar, y la base de datos impide borrar un proveedor que tenga abastecimientos (`DeleteBehavior.Restrict`). Si hubo un error de carga, se corrige con un **ajuste manual de stock** en el formulario del producto: el historial queda intacto. Es el mismo criterio de un libro contable: no se borra un asiento, se hace uno de corrección.

### Cómo funciona el formulario dinámico (`Lineas[i]`) y el model binding

`Views/Abastecimientos/Nuevo.cshtml` + `wwwroot/js/abastecimiento.js` (jQuery):

1. Al elegir el proveedor, el JS pide por AJAX `GET /Abastecimientos/ProductosPorProveedor?proveedorId=…`, que devuelve en JSON solo los productos de sus marcas.
2. El buscador filtra esa lista; al elegir un producto se agrega una **fila** con sus inputs. Si ya estaba, no se duplica: se resalta la fila existente.
3. El resumen (productos, unidades, total) y el "stock actual → stock resultante" se recalculan en vivo, pero es **solo una vista previa**: el servidor vuelve a calcular todo.
4. Antes de enviar, un modal pide confirmación ("Se sumarán 42 unidades al stock de 3 productos. ¿Confirmas?").

El truco está en los **nombres** de los inputs:

```html
<input name="Lineas[0].ProductoId" value="3">
<input name="Lineas[0].Cantidad" value="10">
<input name="Lineas[0].CostoUnitario" value="120.50">
<input name="Lineas[1].ProductoId" value="20">
...
```

El *model binding* de ASP.NET lee esos nombres y arma solo la lista `AbastecimientoFormViewModel.Lineas`: `Lineas[0]` es el primer `LineaAbastecimiento`, `Lineas[1]` el segundo, etc. Los índices deben ser **consecutivos desde 0**: si falta uno (por ejemplo quedan `Lineas[0]` y `Lineas[2]` al quitar la fila del medio), el binder se detiene en el hueco y las filas siguientes se pierden. Por eso la función `reindexar()` renumera todas las filas cada vez que se agrega o quita una.

`RegistradoPor` no está en el formulario: el controlador lo toma de `User.Identity.Name`, así nadie puede registrar un abastecimiento a nombre de otro. Si el servidor rechaza el envío, el formulario vuelve con el error y las filas se vuelven a armar desde `data-lineas-iniciales`.

**Decimales:** un `<input type="number">` siempre envía el punto decimal (`120.50`). Con la cultura `es-BO` (coma decimal) el servidor leía ese punto como separador de miles y guardaba `12050`. Por eso `Program.cs` fija para toda la app una cultura `es-BO` con **punto** decimal (`UseRequestLocalization`), sin depender del idioma de Windows ni del servidor de Render.

### Costo unitario vs. precio

- **`CostoUnitario`** (en `DetalleAbastecimiento`): lo que la tienda **le paga al proveedor** por cada unidad. Es un dato histórico de esa compra y puede cambiar de una entrega a otra.
- **`Price`** / **`PromotionalPrice`** (en `Product`): lo que **paga el cliente** en la tienda.

Son independientes a propósito: abastecer **no cambia** el precio de venta, y el módulo no calcula márgenes ni ganancias. Tampoco suma nada a los reportes de ingresos: un abastecimiento es un gasto de la tienda, no un ingreso.

## Login (ASP.NET Identity)

- `Program.cs` registra Identity (`AddIdentity<ApplicationUser, IdentityRole>`) y siembra los roles `Admin`/`User` al arrancar si no existen.
- `AdminSeeder` crea (o promueve a Admin) la cuenta definida por configuración (`Admin:Email`/`Admin:Password`), nunca hardcodeada.
- Login social: Google y GitHub se registran en `Program.cs` **solo si** hay `ClientId`/`ClientSecret` configurados; si faltan, la app sigue funcionando sin esos botones.
- `AccountController.Login` respeta `returnUrl` (validado con `Url.IsLocalUrl`) y usa `lockoutOnFailure: true`.
- `[Authorize(Roles = "Admin")]` en `AdminController` (y en `ProveedoresController`, `MarcasController` y `AbastecimientosController`) protege todo el panel.

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
