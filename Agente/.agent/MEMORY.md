# Memoria del refactor

Decisiones y detalles no obvios leyendo solo el código actual, para quien retome el proyecto.

- **`GuideEdit`/`TransportEdit` tienen el mismo bug de `CreatedAt` pisado** que tenían `ProductEdit`/`ServiceEdit` (Fase 2 solo corrigió Product/Service, el plan no mencionaba Guide/Transport). Sigue pendiente; ver `TASKS.md`.
- **`Service` no tiene `IsActive`** como `Guide`/`Transport`: por eso `ServiceDelete` no "desactiva" una ruta con reservas, directamente bloquea el borrado y avisa con un toast.
- **`_ProductCard.cshtml` y `_ServiceCardLarge.cshtml` se borraron en la Fase 6**: nunca se renderizaban (la única partial de producto usada es `_ProductoCard`, con "o"). Si aparece un error de vista faltante referenciando alguno de los dos, es una referencia vieja que hay que actualizar a `_ProductoCard`.
- **Reservas solo admite una reserva activa por usuario/ruta** (PK compuesta `UserId`+`ServiceId`, sin campo de fecha de salida). No es un bug del refactor: es una limitación de diseño original, documentada en `TASKS.md`, que requeriría cambiar la PK para soportar múltiples fechas.
- **`AddCartAndFavorites2` es una migración vacía** y se dejó tal cual (no se borra una migración ya aplicada en producción).
- **Región del negocio es Cochabamba, Bolivia** — el código y los datos de seed nunca mencionaron Argentina; la corrección "Argentina → Bolivia" del plan era preventiva, no había texto real que cambiar.
- **`AdminController` se dividió en clases parciales en la Fase 7** solo por tamaño de archivo (34 KB); no cambió ninguna ruta ni comportamiento. El constructor primario (`ApplicationDbContext context, IImageStorageService imageStorage`) vive únicamente en `AdminController.cs`; las demás partes son `partial class AdminController` sin parámetros y usan `context`/`imageStorage` como si fueran campos propios (válido en C# con constructores primarios + partial classes).
