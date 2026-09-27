# Módulo de Proveedores y Abastecimiento — Tren al Sur

> **Instrucciones para Claude Code.** Lee este documento completo antes de escribir código.
> Trabaja **fase por fase**, en orden. Al terminar cada fase: compila, prueba y espera
> confirmación antes de continuar. Es un proyecto universitario: código **simple, claro y comentado**.

---

## 0. Objetivo y alcance

Agregar al **panel de administración** una sección de **Proveedores** que permita:

1. Registrar y administrar proveedores.
2. Relacionar cada proveedor con las **marcas** que distribuye.
3. **Abastecer la tienda**: registrar ingresos de mercadería (qué proveedor entregó qué productos, cuántos y a qué costo), lo que **aumenta el stock** de los productos.
4. Consultar el **historial de abastecimientos** por proveedor, por marca y por producto.

### Lo que SÍ se hace
- CRUD de proveedores (con desactivación, no borrado físico).
- Gestión de marcas (reutilizando lo que exista) y relación proveedor ↔ marca.
- Registro de abastecimientos (cabecera + detalle) que suma stock en una transacción.
- Historial y detalle de cada abastecimiento.
- Indicadores simples en el dashboard: proveedores activos, abastecimientos del mes, productos con stock bajo.

### Lo que NO se hace
- ❌ Portal o login para proveedores (solo el administrador usa el módulo).
- ❌ Órdenes de compra con aprobaciones, cuentas por pagar, facturas o pagos a proveedores.
- ❌ Cálculo de márgenes o ganancias.
- ❌ Sumar abastecimientos a los reportes de ingresos existentes (los reportes no se tocan).
- ❌ Cambios automáticos de precio de venta al abastecer.
- ❌ Reestructurar la arquitectura existente sin proponerlo primero.

---

## FASE 1 — Análisis del código existente (sin modificar nada)

Revisa y resume en el chat:

1. **Producto**: entidad, campos de `Stock`, `Precio`, categoría y **cómo se guarda la marca**:
   - ¿Existe una entidad `Marca`?
   - ¿O la marca es un `string` dentro de `Producto`?
2. **Panel de administración**: layout del dashboard, menú lateral, estilo de tablas, formularios, alertas y cómo se protege (rol `Admin`, `[Authorize(Roles = ...)]`, etc.).
3. **Servicios existentes** de productos y cómo se modifica el stock hoy (por ejemplo, al crear pedidos).
4. **Contexto de EF Core** (`DbContext`), convención de nombres de tablas y migraciones existentes.
5. **Patrones** usados en otros módulos admin (reservas, pedidos, reportes) para imitarlos.

Entrega una lista de archivos a **reutilizar**, **modificar** y **crear**.

> **Decisión importante sobre marcas:**
> - Si `Marca` ya es entidad → reutilizarla.
> - Si la marca es un `string` en `Producto` → **proponer** crear la entidad `Marca`, migrar los valores existentes (una marca por cada texto distinto) y reemplazar el string por `MarcaId`. **No ejecutar este cambio sin confirmación**, porque afecta filtros del catálogo y vistas existentes.

Espera confirmación antes de seguir.

---

## FASE 2 — Modelo de datos

### Diagrama

```
Proveedor 1 ─── * ProveedorMarca * ─── 1 Marca 1 ─── * Producto
    │
    1
    │
    * Abastecimiento 1 ─── * DetalleAbastecimiento * ─── 1 Producto
```

### Entidades

```csharp
public class Proveedor
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Nombre { get; set; } = "";          // Razón social o nombre comercial

    [StringLength(20)]
    public string? Nit { get; set; }

    [StringLength(100)]
    public string? PersonaContacto { get; set; }

    [StringLength(30)]
    public string? Telefono { get; set; }

    [StringLength(120), EmailAddress]
    public string? Email { get; set; }

    [StringLength(80)]
    public string? Ciudad { get; set; }

    [StringLength(200)]
    public string? Direccion { get; set; }

    [StringLength(500)]
    public string? Notas { get; set; }

    public bool Activo { get; set; } = true;           // desactivar en vez de borrar
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public ICollection<ProveedorMarca> Marcas { get; set; } = new List<ProveedorMarca>();
    public ICollection<Abastecimiento> Abastecimientos { get; set; } = new List<Abastecimiento>();
}
```

```csharp
// Solo si NO existe ya una entidad Marca
public class Marca
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Nombre { get; set; } = "";

    public string? LogoUrl { get; set; }                // Supabase Storage, opcional
    public bool Activo { get; set; } = true;

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    public ICollection<ProveedorMarca> Proveedores { get; set; } = new List<ProveedorMarca>();
}
```

```csharp
// Tabla intermedia: qué marcas distribuye cada proveedor
public class ProveedorMarca
{
    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    public int MarcaId { get; set; }
    public Marca Marca { get; set; } = null!;
}
```

```csharp
public class Abastecimiento
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    public string? NumeroComprobante { get; set; }      // nota de entrega o factura del proveedor

    [StringLength(500)]
    public string? Observaciones { get; set; }

    public decimal Total { get; set; }                  // suma de los detalles, calculada en servidor

    public string RegistradoPor { get; set; } = "";     // usuario admin que lo registró

    public ICollection<DetalleAbastecimiento> Detalles { get; set; } = new List<DetalleAbastecimiento>();
}

public class DetalleAbastecimiento
{
    public int Id { get; set; }

    public int AbastecimientoId { get; set; }
    public Abastecimiento Abastecimiento { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }          // precio de compra (NO el de venta)
    public decimal Subtotal { get; set; }               // Cantidad * CostoUnitario
}
```

### Configuración en el `DbContext`

```csharp
modelBuilder.Entity<ProveedorMarca>()
    .HasKey(pm => new { pm.ProveedorId, pm.MarcaId });

modelBuilder.Entity<Proveedor>()
    .HasIndex(p => p.Nombre);

// Precisión de dinero para PostgreSQL
modelBuilder.Entity<Abastecimiento>().Property(a => a.Total).HasPrecision(12, 2);
modelBuilder.Entity<DetalleAbastecimiento>().Property(d => d.CostoUnitario).HasPrecision(12, 2);
modelBuilder.Entity<DetalleAbastecimiento>().Property(d => d.Subtotal).HasPrecision(12, 2);

// Un abastecimiento no se borra si se borra el proveedor
modelBuilder.Entity<Abastecimiento>()
    .HasOne(a => a.Proveedor).WithMany(p => p.Abastecimientos)
    .OnDelete(DeleteBehavior.Restrict);
```

### Migración
1. `dotnet ef migrations add AgregarProveedoresYAbastecimientos`
2. Revisar el archivo generado antes de aplicarlo (sobre todo si incluye la migración de marcas).
3. Aplicar primero en local/desarrollo y luego en Supabase.

---

## FASE 3 — DTOs / ViewModels

Mantenerlos simples. Carpeta: la que ya use el proyecto (`ViewModels/` o `DTOs/`).

```csharp
public class ProveedorFormViewModel
{
    public int? Id { get; set; }
    [Required(ErrorMessage = "El nombre es obligatorio")] public string Nombre { get; set; } = "";
    public string? Nit { get; set; }
    public string? PersonaContacto { get; set; }
    public string? Telefono { get; set; }
    [EmailAddress] public string? Email { get; set; }
    public string? Ciudad { get; set; }
    public string? Direccion { get; set; }
    public string? Notas { get; set; }

    public List<int> MarcasSeleccionadas { get; set; } = new();
    public List<SelectListItem> MarcasDisponibles { get; set; } = new();
}

public class ProveedorListaItem
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? PersonaContacto { get; set; }
    public string? Telefono { get; set; }
    public List<string> Marcas { get; set; } = new();
    public DateTime? UltimoAbastecimiento { get; set; }
    public bool Activo { get; set; }
}

public class AbastecimientoFormViewModel
{
    [Required(ErrorMessage = "Selecciona un proveedor")]
    public int ProveedorId { get; set; }
    public string? NumeroComprobante { get; set; }
    public string? Observaciones { get; set; }
    public List<LineaAbastecimiento> Lineas { get; set; } = new();
}

public class LineaAbastecimiento
{
    [Required] public int ProductoId { get; set; }
    [Range(1, 10000, ErrorMessage = "Cantidad inválida")] public int Cantidad { get; set; }
    [Range(0.01, 1000000, ErrorMessage = "Costo inválido")] public decimal CostoUnitario { get; set; }
}
```

---

## FASE 4 — Servicios (lógica de negocio)

### `IProveedorService`
```csharp
Task<List<ProveedorListaItem>> ListarAsync(string? busqueda, int? marcaId, bool? activo);
Task<Proveedor?> ObtenerAsync(int id);                 // con marcas y últimos abastecimientos
Task<int> CrearAsync(ProveedorFormViewModel model);
Task ActualizarAsync(ProveedorFormViewModel model);   // incluye actualizar marcas
Task CambiarEstadoAsync(int id, bool activo);          // activar / desactivar
```

### `IMarcaService` (solo si no existe)
```csharp
Task<List<Marca>> ListarAsync(bool soloActivas = false);
Task<int> CrearAsync(string nombre, string? logoUrl);
Task ActualizarAsync(int id, string nombre, string? logoUrl);
Task CambiarEstadoAsync(int id, bool activo);
```

### `IAbastecimientoService`
```csharp
Task<int> RegistrarAsync(AbastecimientoFormViewModel model, string usuario);
Task<List<Abastecimiento>> ListarAsync(int? proveedorId, int? marcaId, DateTime? desde, DateTime? hasta);
Task<Abastecimiento?> ObtenerDetalleAsync(int id);
Task<List<Producto>> ProductosPorProveedorAsync(int proveedorId); // productos de las marcas del proveedor
```

### Reglas de `RegistrarAsync` (lo más importante del módulo)

```csharp
public async Task<int> RegistrarAsync(AbastecimientoFormViewModel model, string usuario)
{
    // 1. Validaciones de negocio
    //    - El proveedor existe y está Activo.
    //    - Hay al menos una línea.
    //    - No hay productos repetidos (si los hay, sumar cantidades o rechazar: rechazar es más simple).
    //    - Cada producto existe.
    //    - (Recomendado) Cada producto pertenece a una marca que el proveedor distribuye.

    // 2. Transacción: o se guarda TODO, o no se guarda NADA
    using var transaccion = await _context.Database.BeginTransactionAsync();

    var abastecimiento = new Abastecimiento { ProveedorId = model.ProveedorId, /* ... */ RegistradoPor = usuario };

    foreach (var linea in model.Lineas)
    {
        var producto = await _context.Productos.FindAsync(linea.ProductoId);

        abastecimiento.Detalles.Add(new DetalleAbastecimiento
        {
            ProductoId = linea.ProductoId,
            Cantidad = linea.Cantidad,
            CostoUnitario = linea.CostoUnitario,
            Subtotal = linea.Cantidad * linea.CostoUnitario   // calculado en servidor
        });

        producto!.Stock += linea.Cantidad;                  // <- aquí se abastece la tienda
    }

    abastecimiento.Total = abastecimiento.Detalles.Sum(d => d.Subtotal);

    _context.Abastecimientos.Add(abastecimiento);
    await _context.SaveChangesAsync();
    await transaccion.CommitAsync();

    return abastecimiento.Id;
}
```

Reglas adicionales:
- Los abastecimientos **no se editan ni se eliminan** una vez registrados (así el historial y el stock siempre coinciden). Si hubo un error, se corrige con un ajuste manual de stock en el producto (flujo existente).
- Un proveedor **desactivado** no aparece en el formulario de abastecimiento, pero su historial sigue visible.
- Una marca **no se puede desactivar** si tiene productos activos (mostrar mensaje claro).
- Todo cálculo de totales se hace en el servidor.

---

## FASE 5 — Controladores (área de administración)

Seguir el patrón de los controladores admin existentes (misma área, mismo `[Authorize(Roles = "...")]`).

```
ProveedoresController
    GET  Index(busqueda, marcaId, estado)   → listado con filtros
    GET  Detalle(id)                        → ficha del proveedor
    GET  Crear / POST Crear
    GET  Editar(id) / POST Editar
    POST CambiarEstado(id, activo)

MarcasController            (solo si no existe gestión de marcas)
    GET  Index / GET Crear / POST Crear / GET Editar / POST Editar / POST CambiarEstado

AbastecimientosController
    GET  Index(proveedorId, marcaId, desde, hasta) → historial
    GET  Nuevo(proveedorId?)                        → formulario
    POST Nuevo                                      → registra y redirige a Detalle
    GET  Detalle(id)                                → comprobante del abastecimiento
    GET  ProductosPorProveedor(proveedorId)         → JSON para el formulario (AJAX)
```

Reglas:
- `[ValidateAntiForgeryToken]` en todos los POST.
- Patrón **Post → Redirect → Get** con `TempData["Exito"]` para mensajes.
- Controladores delgados: solo validan `ModelState`, llaman al servicio y devuelven la vista.
- El usuario de `RegistradoPor` se obtiene de `User.Identity.Name`, nunca del formulario.

---

## FASE 6 — Diseño del panel

### Principio
El panel admin es una herramienta de trabajo: **claro, ordenado y rápido de usar**, pero con la misma identidad oscura de Tren al Sur. Menos fotografía que la tienda, más legibilidad.
Si el dashboard ya tiene un estilo propio, **seguirlo**; lo siguiente solo complementa.

### Paleta (reutilizar variables existentes)

```css
--tas-bg: #0B100E;  --tas-section: #111713;  --tas-card: #171E1A;  --tas-border: #303832;
--tas-text: #F1F3F1; --tas-text-muted: #A8B0AA;
--tas-green: #315D45; --tas-green-light: #557F63; --tas-earth: #7C7058;
--tas-warning: #B8923E;  /* stock bajo, tono ocre */
--tas-error: #C9694F;
```

### Menú lateral
Agregar un grupo nuevo:

```
INVENTARIO
  🚚  Proveedores          (bi-truck)
  🏷️  Marcas               (bi-tags)        ← solo si se crea
  📦  Abastecimientos      (bi-box-seam)
```

### Tarjetas de indicadores en el dashboard principal
Tres tarjetas nuevas, con el mismo estilo que las existentes:

| Indicador | Ícono | Detalle |
|---|---|---|
| Proveedores activos | `bi-truck` | número grande |
| Abastecimientos del mes | `bi-box-seam` | cantidad + total en Bs |
| Productos con stock bajo | `bi-exclamation-triangle` | en ocre; enlace a la lista |

> **Stock bajo:** usar el umbral si el proyecto ya tiene uno; si no, proponer una constante simple (por ejemplo `StockMinimo = 5` en un solo lugar) y confirmar el valor.

Debajo, una lista breve **"Últimos abastecimientos"** (5 filas: fecha, proveedor, productos, total) con enlace "Ver todos".

---

## FASE 7 — Vistas de Proveedores

### `Proveedores/Index`
```
┌──────────────────────────────────────────────────────────────┐
│ PROVEEDORES                              [+ Nuevo proveedor]  │
│ Quiénes equipan tu tienda                                     │
├──────────────────────────────────────────────────────────────┤
│ [🔍 Buscar nombre / NIT]  [Marca ▼]  [Estado: Activos ▼]      │
├──────────────────────────────────────────────────────────────┤
│ Nombre      Contacto     Marcas            Último abast.  ⋯   │
│ Andes Gear  Luis Q.      [Marca A][Marca B] 12/09/2026    ⋯   │
│ ...                                                           │
└──────────────────────────────────────────────────────────────┘
```
- Marcas como **chips** pequeños (fondo `--tas-section`, borde `--tas-border`).
- Proveedor inactivo: fila con opacidad reducida y etiqueta "Inactivo".
- Menú de acciones (⋯): Ver, Editar, Abastecer, Activar/Desactivar.
- Desactivar pide confirmación con un modal Bootstrap.
- Estado vacío amigable: ícono de camión + *"Aún no registras proveedores"* + botón.
- En móvil la tabla se convierte en **tarjetas apiladas**.

### `Proveedores/Crear` y `Editar`
Formulario en dos bloques dentro de tarjetas:
1. **Datos del proveedor**: nombre, NIT, contacto, teléfono, email, ciudad, dirección, notas.
2. **Marcas que distribuye**: lista de marcas como **chips seleccionables** (checkbox oculto + label estilizado; seleccionado = borde `--tas-green-light` con ✓). Buscador rápido si hay muchas marcas.

Botones: **Guardar** (verde) y **Cancelar** (secundario).

### `Proveedores/Detalle`
```
┌────────────────────────────────────────────┬──────────────────┐
│ ANDES GEAR                    [Activo]      │ RESUMEN          │
│ NIT · contacto · teléfono · email · ciudad │ Abastecimientos: │
│ Notas                                       │ 14               │
│ Marcas: [chip][chip][chip]                  │ Total comprado:  │
│                                             │ Bs 32.450,00     │
│ [Editar]  [📦 Abastecer]                    │ Último: 12/09    │
├─────────────────────────────────────────────┴──────────────────┤
│ HISTORIAL DE ABASTECIMIENTOS (tabla: fecha, comprobante,       │
│ n.º productos, total, ver)                                     │
└────────────────────────────────────────────────────────────────┘
```
- Botones de contacto rápido: WhatsApp (`https://wa.me/...`) y email (`mailto:`), solo si hay datos.

---

## FASE 8 — Vistas de Abastecimiento

### `Abastecimientos/Nuevo` (la pantalla más importante)

```
┌──────────────────────────────────────────────────────────────┐
│ ABASTECER TIENDA                                              │
│ Registra el equipo que llega a tu tienda                      │
├───────────────────────────────────────┬──────────────────────┤
│ 1. PROVEEDOR                           │ RESUMEN (sticky)      │
│ [Selecciona proveedor ▼]               │ Productos: 3          │
│ Marcas: [chip][chip]                   │ Unidades: 42          │
│ Comprobante n.º [______]               │ Total:  Bs 8.940,00   │
│                                        │                       │
│ 2. PRODUCTOS                           │ [Registrar            │
│ [🔍 Buscar producto de estas marcas]   │  abastecimiento]      │
│ ┌────────────────────────────────────┐ │                       │
│ │img Producto  Stock  Cant.  Costo  Sub│ │                       │
│ │🎒 Mochila 40L  3→13 [10] [120] 1200 ✕│ │                       │
│ └────────────────────────────────────┘ │                       │
│ 3. OBSERVACIONES [textarea]            │                       │
└───────────────────────────────────────┴──────────────────────┘
```

Comportamiento (`wwwroot/js/abastecimiento.js`, jQuery):
1. Al elegir el proveedor, se muestran sus marcas y se cargan por AJAX **solo los productos de esas marcas**.
2. El buscador filtra esa lista; al elegir un producto se agrega una fila.
3. Cada fila muestra miniatura, nombre, marca, **stock actual → stock resultante** (en verde) y campos de cantidad y costo unitario.
4. No se puede agregar el mismo producto dos veces (si ya está, se enfoca su fila).
5. Subtotales y resumen se recalculan en vivo (solo como vista previa; el servidor recalcula todo).
6. El botón de registrar se habilita cuando hay proveedor y al menos una línea válida.
7. Antes de enviar, un **modal de confirmación**: *"Se sumarán 42 unidades al stock de 3 productos. ¿Confirmas?"*
8. Los inputs de las filas usan nombres indexados para el model binding: `Lineas[0].ProductoId`, `Lineas[0].Cantidad`, `Lineas[0].CostoUnitario`. Al eliminar una fila, **reindexar** los nombres.
9. Si se llega desde el detalle de un proveedor (`Nuevo?proveedorId=5`), el proveedor viene preseleccionado.

### `Abastecimientos/Index` (historial)
- Filtros: proveedor, marca, rango de fechas.
- Tabla: fecha, proveedor, comprobante, n.º productos, unidades, total, registrado por, ver.
- Fila de totales al final del período filtrado.
- Filtrar por marca muestra los abastecimientos que incluyen productos de esa marca.

### `Abastecimientos/Detalle`
Estilo de **comprobante**:
- Encabezado: "Abastecimiento #0015", fecha, proveedor, comprobante, registrado por.
- Tabla de productos con cantidad, costo unitario y subtotal.
- Total destacado.
- Botón **Imprimir** (reutilizar el enfoque de impresión que ya usan los reportes, con `@media print` en fondo blanco).

---

## FASE 9 — Marcas (solo si se crea la entidad)

### `Marcas/Index`
- Grid de tarjetas (no tabla): logo o inicial de la marca, nombre, n.º de productos y n.º de proveedores.
- Acciones: Editar, Activar/Desactivar.
- Logo opcional subido a **Supabase Storage** usando el mismo servicio de subida de imágenes que ya usan los productos.

### Integración con productos
- El formulario de crear/editar producto cambia el campo de marca de texto a un **select** de marcas activas.
- Los filtros por marca del catálogo público pasan a usar `MarcaId` (verificar que sigan funcionando).

---

## FASE 10 — Relación con el stock del catálogo

- El catálogo público **no cambia visualmente**: solo refleja el nuevo stock.
- Si el proyecto ya oculta o marca productos sin stock, al abastecer vuelven a estar disponibles automáticamente.
- En la lista de productos del admin, agregar (si no existe) un indicador de stock bajo con el color ocre y un botón rápido **"Abastecer"** que lleve a `Abastecimientos/Nuevo`.

---

## FASE 11 — Responsive

| Pantalla | Comportamiento |
|---|---|
| Desktop | Tablas completas; resumen de abastecimiento lateral y fijo. |
| Tablet | Tablas con scroll horizontal interno si hace falta; resumen arriba. |
| Móvil | Listados como tarjetas; filtros dentro de un botón "Filtros" (offcanvas); cada línea de abastecimiento como tarjeta; botón "Registrar" fijo abajo con el total. |

Áreas táctiles de 44–48px y `inputmode="numeric"` / `decimal` en cantidades y costos.

---

## FASE 12 — Animaciones (sutiles)

- Aparición de tarjetas: fade + `translateY(8px)`, 0.3s.
- Nueva fila de producto: fade-in; al eliminarla, fade-out.
- Cambio de stock resultante: transición suave del número.
- Hover en filas y tarjetas: fondo ligeramente más claro.
- Respetar `prefers-reduced-motion`.

---

## FASE 13 — Seguridad y consistencia

- [ ] Todo el módulo protegido con el rol de administrador existente.
- [ ] `[ValidateAntiForgeryToken]` en todos los POST.
- [ ] El abastecimiento se guarda en **una transacción** (detalles + stock + cabecera).
- [ ] Totales y subtotales calculados en el servidor.
- [ ] Cantidades y costos validados en servidor (positivos, límites razonables).
- [ ] No se puede abastecer con un proveedor inactivo ni con productos inexistentes.
- [ ] `RegistradoPor` sale del usuario autenticado.
- [ ] Los abastecimientos no tienen acciones de editar/eliminar.
- [ ] Los reportes de ingresos existentes **no cambian**.

---

## FASE 14 — Pruebas manuales

1. Crear proveedor con 2 marcas → aparece en la lista con sus chips.
2. Crear proveedor sin nombre → error de validación.
3. Filtrar proveedores por marca y por estado.
4. Desactivar proveedor → no aparece en "Abastecer", su historial sigue visible.
5. Abastecer 10 unidades de un producto con stock 3 → queda en 13 (verificar en BD y en el catálogo).
6. Abastecer varios productos a la vez → el total coincide con la suma de subtotales.
7. Intentar agregar el mismo producto dos veces → no se duplica la fila.
8. Enviar cantidad 0 o costo negativo (manipulando el HTML) → el servidor lo rechaza.
9. Forzar un error a mitad del guardado → no cambia ningún stock (transacción).
10. Producto con stock 0 oculto/marcado en la tienda → tras abastecer vuelve a estar disponible.
11. Ver e imprimir el detalle de un abastecimiento.
12. Revisar las pantallas en móvil (375px) y tablet (768px).
13. Si se migró la marca: el catálogo sigue filtrando por marca correctamente.

---

## FASE 15 — Documentación

Agregar una sección en `Agente/GUIA_DE_ESTUDIO.md` explicando en lenguaje sencillo:
- El modelo de datos (proveedor, marca, abastecimiento, detalle) y sus relaciones.
- Qué es una **relación muchos a muchos** y por qué existe `ProveedorMarca`.
- Qué es una **transacción** y por qué el abastecimiento la necesita.
- Por qué los abastecimientos no se editan ni eliminan.
- Cómo funciona el formulario dinámico con `Lineas[i]` y el model binding.
- Diferencia entre **costo unitario** (compra al proveedor) y **precio** (venta al cliente).

---

## Resumen para Claude Code

1. Analiza primero; **decide con el usuario** qué hacer con la marca si hoy es texto.
2. Proveedores con desactivación, no borrado.
3. Proveedor ↔ Marca es muchos a muchos.
4. Abastecer = cabecera + detalles + suma de stock, **todo en una transacción**.
5. El servidor calcula y valida todo; el JavaScript solo ayuda a la experiencia.
6. Mismo estilo del panel admin, con la identidad oscura de Tren al Sur.
7. Código simple y comentado; trabaja por fases y espera confirmación entre cada una.
