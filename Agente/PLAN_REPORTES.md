# Plan de Carrito, Pedidos, Reservas y Reportes — Tren al Sur

> **Para Claude Code.** Lee este archivo completo antes de empezar.
> El proyecto es universitario y el dueño lo va a **estudiar después**. Manda la simplicidad sobre la elegancia.

## Objetivo y orden de trabajo

Los reportes solo son confiables si antes existen los datos correctos. Por eso el orden es:

1. Carrito que funcione bien (sin cambios de BD).
2. Pedidos: el carrito termina en un pedido (hoy no existe `Order`).
3. Reservas reales para las rutas (hoy solo las crea el admin y el modelo tiene fallas).
4. Reportes, calculados sobre esos datos.

Este plan **reemplaza** las restricciones del plan anterior ("no implementes reservas", "no tocar la PK de `Reservation`"). Ahora sí se hacen, siguiendo las decisiones de abajo.

## Decisiones de negocio ya tomadas (no las cambies sin preguntarme)

1. **Ingreso solo cuando termina.** El dinero se registra únicamente cuando la reserva está en `Acabado` o el pedido en `Entregado`. Antes de eso, el monto se muestra aparte como **"por confirmar"**. Los `Cancelado` nunca cuentan.
2. **Fecha del ingreso = fecha en que se finalizó** (`CompletedAt` / `DeliveredAt`), no la de creación ni la de la salida.
3. **Sin pagos en línea.** El pedido y la reserva son solicitudes; el cobro se coordina fuera del sistema y el admin cambia los estados. No agregues pasarelas ni campos de pago.
4. **Estados de reserva** (los actuales, no cambian el `RegularExpression`): `Pendiente` (solicitada), `Recorrido` (en curso), `Acabado` (finalizada), `Cancelado`.
   - Transiciones permitidas: `Pendiente → Recorrido → Acabado`; `Pendiente` o `Recorrido → Cancelado`.
   - `Acabado` y `Cancelado` son finales.
5. **Estados de pedido:** `Pendiente → Entregado`, o `Pendiente → Cancelado`. Ambos finales.
6. **Stock:** se descuenta al crear el pedido y se devuelve si se cancela.
7. **Reservas:** el cliente elige libremente la **fecha de salida** (desde mañana). No hay salidas programadas. La capacidad por fecha usa `Service.MaxGroupSize`, sumando las personas de reservas no canceladas de esa ruta y esa fecha.
8. **Zona horaria:** Bolivia es UTC−4 sin horario de verano. Para agrupar por día o mes, resta 4 horas a los timestamps UTC con una constante, sin depender de la base de datos de zonas horarias del servidor.

## Reglas (valen en todas las fases)

1. **Código simple.** Sin repositorios, MediatR, AutoMapper, CQRS ni paquetes NuGet nuevos. Servicios con LINQ básico y comentarios breves en español sobre cada método.
2. Respeta `Agente/AGENTS.md` y `Agente/.agent/*.md`. Si este plan los contradice, avísame antes de actuar.
3. **No inventes funcionalidades** fuera de este plan. Si algo requiere una decisión de diseño, pregúntame.
4. **No toques:** cadenas de conexión, valores de Supabase/Google/GitHub, el framework, ni los `DataAnnotations` existentes. Nunca imprimas secretos.
5. **Dinero:** siempre `decimal` con precisión `(18,2)`. Precios y totales se calculan **en el servidor**; nunca confíes en valores que vengan del formulario.
6. **Fechas:** los timestamps son `DateTime` UTC (Npgsql exige `Kind=Utc`). Para la fecha de salida usa `DateOnly`. Los filtros `desde/hasta` son inclusivos, y el fin de rango en consultas se trata como exclusivo (`hasta + 1 día`).
7. **Migraciones:** en las Fases 2 y 3 *crea* los archivos de migración, muéstrame el SQL (`dotnet ef migrations script`) y **detente**. **Nunca ejecutes `dotnet ef database update`**: la BD es la de producción, hago un respaldo y la aplico yo.
8. No borres archivos sin mostrarme antes la lista con evidencia y esperar mi "sí".
9. Rama `reportes`. Un commit por fase. **No hagas push.**
10. Ahorra tokens: usa `grep`, no pegues archivos completos en pantalla.
11. **Al terminar cada fase:**
    - `dotnet build` con 0 errores. Si falla por un bloqueo de archivo, avísame que cierre la app en Visual Studio.
    - Resumen corto de lo cambiado.
    - Páginas y escenarios a probar en el navegador (perfil `http`, puerto 5187).
    - Marca las tareas como `[x]` en este archivo.
    - **Detente y espera mi OK.**

---

## Fase 0 — Preparación

- [ ] Lee `Agente/AGENTS.md` y `Agente/.agent/*.md`; resume en 5 líneas lo que debes respetar.
- [ ] `git status` limpio; crea la rama `reportes`.
- [ ] `dotnet build` de referencia.
- [ ] Recuérdame hacer un **respaldo de la BD de Supabase** antes de aplicar las migraciones de las Fases 2 y 3.

## Fase 1 — Carrito correcto (sin migración)

- [x] `CartController`: acción `Update` (POST) para cambiar la cantidad con botones − y +. Validar que sea ≥ 1, ≤ stock y ≤ 100. Si llega a 0, ofrecer "Quitar".
- [x] **Precio vigente:** el carrito muestra y suma `PromotionalPrice ?? Price` actual del producto, no el `UnitPrice` guardado al agregar (queda obsoleto si el precio cambia). Deja la columna `CartItem.UnitPrice` como está; se anotará como heredada en la guía de estudio.
- [x] Al mostrar el carrito, avisar por ítem si el producto quedó **sin stock** o la cantidad supera el stock, e impedir continuar hasta corregirlo.
- [x] Vista del carrito: subtotal por línea, total general y enlace "Seguir comprando". Botón "Confirmar pedido" **deshabilitado** con nota "Disponible en la siguiente fase"; se activa en la Fase 2.
- [x] Navbar: mostrar un contador del carrito **solo si es simple** (un `ViewComponent` pequeño). Si complica el código, omítelo y dímelo.

## Fase 2 — Pedidos (migración, confirmar antes de crearla)

- [x] `Models/Order.cs`: `Id`, `UserId`, `CreatedAt`, `Status` (`Pendiente|Entregado|Cancelado`), `Total` (snapshot calculado en servidor), `DeliveredAt` (`DateTime?`).
- [x] `Models/OrderItem.cs`: `Id`, `OrderId`, `ProductId` (`int?`, FK con `SetNull` para que borrar un producto no rompa el historial), `ProductName`, `ProductCategory`, `UnitPrice`, `Quantity`. Los tres primeros datos son **snapshot** del momento del pedido.
- [x] `ApplicationDbContext`: `DbSet<Order>`, `DbSet<OrderItem>`, precisión `(18,2)`, FKs. Índice **único** `(UserId, ProductId)` en `CartItems`. Si ya hay duplicados, avísame antes.
- [x] `OrdersController` (`[Authorize]`):
  - `Checkout` (POST): en **una transacción**, revalida stock, crea `Order` + `OrderItem`s con precio vigente, descuenta stock y vacía el carrito.
  - `Index`: "Mis pedidos".
  - `Details`: detalle de un pedido propio (verificar que sea del usuario).
  - `Cancel`: solo si está `Pendiente`; devuelve el stock.
- [x] Activar el botón "Confirmar pedido" del carrito. Vistas `Views/Orders/` con el mismo estilo y layout público. Enlace "Mis pedidos" en la navbar.
- [x] `AdminController.Orders.cs`: listado con filtro por estado, detalle y cambio de estado.
  - `Pendiente → Entregado` guarda `DeliveredAt = DateTime.UtcNow`.
  - `Pendiente → Cancelado` devuelve el stock.
  - No permitir salir de un estado final.
  - Badge de "Pedidos pendientes" en el sidebar y contador en el dashboard.
- [x] `ProductDelete` en admin debe seguir funcionando con pedidos existentes (por el `SetNull`).
- [x] Genera la migración, **muéstrame el SQL y detente**. No la apliques.

## Fase 3 — Reservas reales (migración, confirmar antes de crearla)

Problemas de hoy: la clave `(UserId, ServiceId)` permite una sola reserva por ruta y no existe fecha de salida.

- [ ] `Reservation`: nueva PK `Id` (identity), `TripDate` (`DateOnly`, fecha de salida), `CompletedAt` (`DateTime?`). Índice **único** `(UserId, ServiceId, TripDate)`. Conserva `BookingDate`, `PeopleCount`, `UnitPrice`, `TotalPrice` y `Status` con sus anotaciones.
- [ ] **Migración cuidadosa:** la tabla ya tiene filas, cambia de PK y el SQL debe dar valor a `TripDate` en las existentes (usa la fecha de `BookingDate`). Revisa el SQL generado, muéstramelo y **detente**. No la apliques.
- [ ] Actualizar `AdminController` (Reservas) a la nueva clave: rutas y vistas con `id` en vez de `userId + serviceId`. Formularios crear/editar con `TripDate`.
- [ ] `ReservationsController` (`[Authorize]`):
  - `Create` (POST): valida ruta `Active`, `TripDate` desde mañana, personas de 1 a `MaxGroupSize` y la **capacidad de esa fecha** (decisión 7). El servidor asigna `UnitPrice = Service.Price`, `TotalPrice = UnitPrice × PeopleCount`, `Status = "Pendiente"`, `BookingDate = UtcNow`.
  - `Index`: "Mis reservas", con estado, fecha de salida y total.
  - `Cancel`: solo reservas propias en `Pendiente`.
- [ ] `Services/Details.cshtml`: reemplazar el botón deshabilitado por un formulario (fecha + personas) para usuarios con sesión. Para anónimos: "Inicia sesión para reservar", con `returnUrl` a esa misma página. Mostrar el total estimado.
- [ ] **Cambio de estado en admin** (`ReservationUpdateStatus`): aplicar las transiciones de la decisión 4. Al pasar a `Acabado`, guardar `CompletedAt = UtcNow`. Rechazar transiciones inválidas con mensaje claro.
- [ ] **No permitir borrar** una reserva `Acabado` (representa ingreso). Sí las `Pendiente` y `Cancelado`.
- [ ] Enlace "Mis reservas" en la navbar y badge de pendientes en el admin (ya existe).

## Fase 4 — Base de reportes y los 4 reportes principales

- [ ] `Services/ReportService.cs` registrado con `AddScoped`, con un método por reporte que devuelve un ViewModel simple en `Models/Reports/`. Consultas con `AsNoTracking()` y agrupación en la BD cuando sea posible; para agrupar por mes con la resta de 4 horas, filtra primero y agrupa en memoria.
- [ ] `AdminController.Reports.cs` y ítem **"Reportes"** en el sidebar de `_AdminLayout`. Página índice con una tarjeta por reporte. Filtros comunes: `desde`, `hasta` (por defecto, el mes en curso).
- [ ] **Ingresos:** filtrados por `CompletedAt` (reservas `Acabado`) y `DeliveredAt` (pedidos `Entregado`). Muestra:
  - total de ingresos por reservas, por productos y total general;
  - tabla por mes;
  - caja aparte **"Por confirmar"**: reservas `Pendiente` y `Recorrido` más pedidos `Pendiente`, con su monto, **excluido** de los ingresos.
- [ ] **Reservas:** filtros por rango de fecha de salida, estado y ruta. Muestra:
  - total y cantidad por estado, y tasa de cancelación;
  - tabla por ruta (cantidad, personas, monto solo de `Acabado`);
  - tabla por mes.
- [ ] **Ventas de productos:** pedidos `Entregado` en el rango por `DeliveredAt`. Tabla por producto (unidades y monto, usando `ProductName` del snapshot) y por categoría (`ProductCategory`).
- [ ] **Inventario** (sin filtros de fecha): stock bajo (≤ 5, igual que el dashboard) y agotado, valor del inventario (`stock × precio vigente`), productos por categoría y marca, productos en oferta.
- [ ] Cada reporte: tarjetas de totales arriba, tabla debajo, mensaje claro si no hay datos.

## Fase 5 — Reportes de apoyo

- [ ] **Demanda:** productos y rutas más guardados en favoritos (`FavoriteItems`, cruzando por `Type` + `ItemId`) y productos más presentes en carritos actuales. Aclara en pantalla que los carritos muestran interés, no compras.
- [ ] **Usuarios:** registros por mes (`ApplicationUser.CreatedAt`, con la resta de 4 horas).
- [ ] **Guías y transportes:** rutas asignadas a cada uno y reservas `Acabado` por guía.

## Fase 6 — Exportar e imprimir

- [ ] **CSV** por reporte, sin paquetes: `StringBuilder` + `File(...)`, separador `;` y UTF-8 con BOM (para que Excel en español lo abra bien). Si una celda empieza con `=`, `+`, `-` o `@`, anteponer `'` para evitar fórmulas.
- [ ] **Imprimir / PDF:** `@media print` (sin sidebar ni botones, fondo claro) y botón "Imprimir / Guardar como PDF". No agregues librerías de PDF.
- [ ] **Gráficos (opcional, pregúntame antes):** Chart.js por CDN, un gráfico por reporte con los datos ya en la vista. Sin llamadas AJAX.

## Fase 7 — Cierre

- [ ] Actualizar `GUIA_DE_ESTUDIO.md`: nuevas entidades (`Order`, `OrderItem`, `Reservation` con `Id`), el flujo carrito → pedido, el flujo de reserva, la regla de ingresos (decisión 1) y cómo se calculan los reportes.
- [ ] Actualizar `Agente/.agent/PROJECT_STATE.md`, `TASKS.md` y `MEMORY.md`. No incluyas correos ni credenciales.
- [ ] Revisión final: `dotnet build` con 0 errores, `git status` limpio y resumen de pendientes.

---

## Escenarios de prueba clave (los pruebo yo en el navegador)

**Carrito y pedidos**
- Cambiar cantidad por encima del stock → se rechaza.
- Confirmar pedido → el stock baja y el carrito queda vacío.
- Cancelar un pedido `Pendiente` → el stock se devuelve.
- Borrar un producto con pedidos → el historial sigue mostrando su nombre.
- Marcar `Entregado` → recién ahí aparece en Ingresos. Antes aparece en "Por confirmar".

**Reservas**
- Reservar 13 personas en una ruta con `MaxGroupSize = 12` → rechazado.
- Dos reservas de la misma ruta en fechas distintas → permitido.
- La misma ruta y fecha dos veces por el mismo usuario → rechazado.
- Fecha pasada o de hoy → rechazada.
- `Pendiente` → `Acabado` directo → rechazado. `Recorrido` → `Acabado` → correcto y guarda `CompletedAt`.
- Una reserva `Acabado` no se puede borrar.
- Ingresos: una reserva `Cancelado` nunca suma; una `Recorrido` sigue en "Por confirmar".

**Reportes**
- Un ingreso finalizado a las 22:00 hora de Bolivia (02:00 UTC del día siguiente) cae en el día y el mes correctos.
- El CSV abre bien en Excel en español, con tildes y `;`.
