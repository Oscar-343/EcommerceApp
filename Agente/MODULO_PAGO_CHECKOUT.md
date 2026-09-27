# Módulo de Checkout y Pago (simulado) — Tren al Sur

> **Instrucciones para Claude Code.** Lee este documento completo antes de escribir código.
> Trabaja **fase por fase**, en orden. Al terminar cada fase: compila, prueba en el navegador y
> espera confirmación antes de pasar a la siguiente.

---

## 0. Objetivo y alcance

Construir la **experiencia visual completa de compra**: carrito → datos de entrega → método de pago → confirmación.

**El pago es SIMULADO.** No hay pasarela real, no se cobra dinero y no se procesa ninguna tarjeta.
La interfaz debe verse y comportarse como una tienda profesional, pero internamente solo simula la respuesta.

### Lo que SÍ se hace
- Página de checkout con pasos (stepper).
- Formulario de datos de entrega/contacto.
- Selector de método de pago: **Tarjeta**, **QR** y **Transferencia bancaria** (solo interfaz).
- Formulario de tarjeta con vista previa animada, máscaras y validación en el navegador.
- Pantalla "Procesando pago…" y página de confirmación.
- Al confirmar, se crea el **pedido** usando el flujo de pedidos que **ya existe** en el proyecto.

### Lo que NO se hace
- ❌ Integrar Stripe, PayPal, Libélula u otra pasarela.
- ❌ Guardar, registrar o enviar al servidor números de tarjeta, CVV o fechas de vencimiento.
- ❌ Usar logos oficiales de bancos o marcas de tarjeta (usar íconos genéricos o texto: "VISA", "MASTERCARD").
- ❌ Crear nuevas reglas de negocio no pedidas (cupones, envíos pagados por zona, impuestos, cuotas, etc.).
- ❌ Reestructurar la arquitectura existente.

### Regla de negocio que ya existe y debe respetarse
El monto de un pedido **solo cuenta como ingreso cuando el pedido está Entregado**. Un pago simulado
**no** cambia eso: el pedido nace en el estado inicial que ya usa el sistema.

---

## FASE 1 — Análisis del código existente (sin modificar nada)

Antes de escribir código, revisa y resume en el chat:

1. **Carrito**: modelo, servicio, controlador y vistas actuales. ¿Dónde se guarda (sesión, BD)? ¿Cómo se calcula el total?
2. **Pedidos**: entidad `Pedido` (o equivalente), detalle, estados existentes y el método del servicio que crea un pedido desde el carrito.
3. **Autenticación**: ¿el checkout exige usuario logueado? Respeta lo que ya hace el flujo de pedidos.
4. **Layout y estilos**: `_Layout.cshtml`, archivo CSS principal, variables de color ya definidas, fuentes y cómo se cargan scripts por vista (`@section Scripts`).
5. **Formato de moneda** usado en el resto de la tienda (reutilizarlo, no inventar otro).

Entrega: una lista corta de archivos que se van a **reutilizar**, **modificar** y **crear**. Espera confirmación.

---

## FASE 2 — Estructura de archivos

Respetar la estructura actual (proyecto MVC único). Crear solo lo necesario:

```
Controllers/
    CheckoutController.cs          ← nuevo
Services/
    IPagoService.cs                ← nuevo
    PagoSimuladoService.cs         ← nuevo
ViewModels/   (o la carpeta que ya use el proyecto para modelos de vista)
    CheckoutViewModel.cs           ← nuevo
    ResumenCompraViewModel.cs      ← nuevo
Views/Checkout/
    Index.cshtml                   ← página principal del checkout
    Confirmacion.cshtml            ← compra completada
    _ResumenCompra.cshtml          ← parcial: resumen lateral
    _PasoEntrega.cshtml            ← parcial: datos de entrega
    _PasoPago.cshtml               ← parcial: métodos de pago
    _TarjetaPreview.cshtml         ← parcial: tarjeta visual animada
wwwroot/css/checkout.css           ← estilos solo del checkout
wwwroot/js/checkout.js             ← lógica del stepper y validaciones
```

Registrar el servicio en `Program.cs`:

```csharp
builder.Services.AddScoped<IPagoService, PagoSimuladoService>();
```

---

## FASE 3 — ViewModels

Mantenerlos simples. **Ningún ViewModel contiene datos de tarjeta.**

```csharp
public class CheckoutViewModel
{
    // Datos de entrega / contacto
    [Required(ErrorMessage = "Ingresa tu nombre completo")]
    [StringLength(100)]
    public string NombreCompleto { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu teléfono")]
    [Phone]
    public string Telefono { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ingresa la ciudad")]
    public string Ciudad { get; set; } = "";

    [Required(ErrorMessage = "Ingresa la dirección")]
    [StringLength(200)]
    public string Direccion { get; set; } = "";

    [StringLength(300)]
    public string? Referencia { get; set; }

    // Pago (solo lo que se puede mostrar)
    [Required]
    public string MetodoPago { get; set; } = "Tarjeta";   // Tarjeta | QR | Transferencia

    public string? TarjetaMarca { get; set; }             // "VISA", "MASTERCARD"... (solo para mostrar)
    public string? TarjetaUltimos4 { get; set; }          // "4242" (solo para mostrar)

    // Resumen (se llena en el servidor, nunca desde el formulario)
    public ResumenCompraViewModel Resumen { get; set; } = new();
}

public class ResumenCompraViewModel
{
    public List<ItemResumen> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public int CantidadProductos => Items.Sum(i => i.Cantidad);
}

public class ItemResumen
{
    public string Nombre { get; set; } = "";
    public string? ImagenUrl { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => PrecioUnitario * Cantidad;
}
```

> Si ya existen DTOs/ViewModels del carrito que sirvan para el resumen, **reutilizarlos** en lugar de crear `ItemResumen`.
> No agregar costos de envío ni impuestos: `Total = Subtotal` salvo que el proyecto ya tenga otra regla.

---

## FASE 4 — Servicio de pago simulado

La idea es que, si algún día se integra una pasarela real, solo se cree otra clase que implemente `IPagoService`.

```csharp
public interface IPagoService
{
    Task<ResultadoPago> ProcesarAsync(string metodoPago, decimal monto);
}

public record ResultadoPago(bool Exitoso, string CodigoTransaccion, string Mensaje);
```

```csharp
public class PagoSimuladoService : IPagoService
{
    private static readonly string[] MetodosValidos = { "Tarjeta", "QR", "Transferencia" };

    public async Task<ResultadoPago> ProcesarAsync(string metodoPago, decimal monto)
    {
        if (!MetodosValidos.Contains(metodoPago))
            return new ResultadoPago(false, "", "Método de pago no válido.");

        if (monto <= 0)
            return new ResultadoPago(false, "", "El monto no es válido.");

        await Task.Delay(1200); // simula la espera de una pasarela

        var codigo = $"TAS-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        return new ResultadoPago(true, codigo, "Pago simulado aprobado.");
    }
}
```

---

## FASE 5 — CheckoutController

```csharp
[Authorize] // solo si el flujo de pedidos actual ya lo exige
public class CheckoutController : Controller
{
    // Inyectar: servicio de carrito existente, servicio de pedidos existente, IPagoService

    [HttpGet]
    public IActionResult Index()
    {
        // 1. Obtener el carrito. Si está vacío → RedirectToAction("Index", "Carrito")
        // 2. Construir el Resumen DESDE EL SERVIDOR (precios de la BD, no del navegador)
        // 3. Prellenar Nombre/Email si el usuario está logueado
        // 4. return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pagar(CheckoutViewModel model)
    {
        // 1. Volver a leer el carrito y recalcular el total en el servidor
        // 2. Si ModelState no es válido → return View("Index", model) con el resumen recargado
        // 3. Validar TarjetaUltimos4: solo 4 dígitos o null. Descartar cualquier otro valor.
        // 4. var resultado = await _pagoService.ProcesarAsync(model.MetodoPago, total);
        // 5. Si falla → ModelState.AddModelError("", resultado.Mensaje) y volver a la vista
        // 6. Crear el pedido con el MÉTODO QUE YA EXISTE en el servicio de pedidos
        //    (guardar método de pago y código de transacción SOLO si la entidad ya lo permite;
        //     si hay que agregar columnas, proponerlo primero y crear la migración de EF Core)
        // 7. Vaciar el carrito (con el método existente)
        // 8. TempData["CodigoTransaccion"] = resultado.CodigoTransaccion;
        // 9. return RedirectToAction(nameof(Confirmacion), new { id = pedido.Id });
    }

    [HttpGet]
    public IActionResult Confirmacion(int id)
    {
        // Mostrar el pedido SOLO si pertenece al usuario actual; si no → NotFound()
    }
}
```

Reglas:
- Patrón **Post → Redirect → Get** para evitar pedidos duplicados al recargar.
- El controlador **no** tiene lógica de negocio: delega en los servicios.
- Nunca confiar en precios, totales o IDs que vengan del formulario.

---

## FASE 6 — Diseño general del checkout

### Concepto
Esta página es la etapa **"Prepárate para salir"** del hilo conductor de Tren al Sur.
Debe sentirse tranquila, ordenada y confiable: menos imágenes que el catálogo, más aire y claridad.
La estética outdoor se mantiene con detalles sutiles, no con decoración excesiva.

### Paleta (reutilizar variables si ya existen en el CSS del proyecto)

```css
:root {
    --tas-bg: #0B100E;
    --tas-section: #111713;
    --tas-card: #171E1A;
    --tas-border: #303832;
    --tas-text: #F1F3F1;
    --tas-text-muted: #A8B0AA;
    --tas-green: #315D45;
    --tas-green-light: #557F63;
    --tas-earth: #7C7058;
    --tas-error: #C9694F;   /* tono tierra rojizo, no rojo chillón */
    --tas-radius: 14px;
}
```

### Estructura de la página (desktop)

```
┌──────────────────────────────────────────────────────────────────┐
│  Banda superior: foto de campamento/amanecer, oscurecida (~180px) │
│  "PREPÁRATE PARA SALIR"                                           │
│  Revisa tu equipo y completa tu pedido.                           │
├──────────────────────────────────────────────────────────────────┤
│  Stepper:  ① Carrito ─── ② Entrega ─── ③ Pago ─── ④ Listo         │
├───────────────────────────────────────┬──────────────────────────┤
│  COLUMNA IZQUIERDA (col-lg-7)          │ COLUMNA DERECHA (col-lg-5)│
│                                        │  (sticky top)             │
│  [Paso 2] Datos de entrega             │  TU EQUIPO                │
│  [Paso 3] Método de pago               │  miniatura · nombre · x2  │
│                                        │  miniatura · nombre · x1  │
│  [Botón] Pagar Bs XXX                  │  ───────────────          │
│                                        │  Subtotal                 │
│                                        │  Total (grande)           │
│                                        │  🔒 Modo demostración     │
└───────────────────────────────────────┴──────────────────────────┘
```

### Stepper
- Círculos numerados unidos por una línea fina (`--tas-border`).
- Paso completado: círculo verde `--tas-green` con ✓.
- Paso actual: borde `--tas-green-light` y texto `--tas-text`.
- Paso pendiente: texto `--tas-text-muted`.
- Nombres con sentido de aventura (opcional): *Equipo · Destino · Pago · En camino*.
- Los pasos 2 y 3 se manejan **en la misma página con JavaScript** (sin recargar). El paso 1 es el carrito existente y el 4 es la página de confirmación.

### Tarjetas de formulario
- Fondo `--tas-card`, borde `1px solid --tas-border`, radio `--tas-radius`, padding generoso (28–32px).
- Título de sección en mayúsculas pequeñas con espaciado de letras (`letter-spacing: .12em`) e ícono simple (Bootstrap Icons: `bi-geo-alt`, `bi-credit-card`).
- Inputs: fondo `--tas-section`, borde `--tas-border`, texto claro, altura mínima 48px.
- Foco: borde `--tas-green-light` y `box-shadow` muy suave del mismo verde (sin brillo neón).
- Etiquetas siempre visibles arriba del campo (no solo placeholder).
- Errores debajo del campo en `--tas-error`, texto pequeño.

### Resumen lateral ("TU EQUIPO")
- Miniaturas cuadradas 64px con radio 10px, nombre, cantidad y subtotal.
- Máximo ~4 ítems visibles; si hay más, scroll interno suave.
- Total en tamaño grande, peso 700.
- Enlace discreto "Editar carrito" que vuelve al carrito.
- Nota inferior con ícono de candado: *"Modo demostración — no se realizan cargos reales."*

---

## FASE 7 — Paso "Datos de entrega"

Campos: Nombre completo, Teléfono, Email, Ciudad, Dirección, Referencia (opcional).

- Grid Bootstrap: nombre a ancho completo; teléfono + email en dos columnas; ciudad + dirección; referencia en `textarea` de 2 filas.
- Botón **"Continuar al pago"** (verde, ancho completo en móvil).
- Al pulsarlo, `checkout.js` valida los campos (jQuery Validation unobtrusive, que ya trae la plantilla MVC). Si todo está bien:
  - Marca el paso 2 como completado.
  - Colapsa la sección mostrando un resumen de una línea ("Juan Pérez · Cochabamba · Av. ...") con un enlace **"Editar"**.
  - Abre la sección de pago con una transición suave (fade + desplazamiento vertical de 12px).

---

## FASE 8 — Paso "Método de pago"

### Selector de métodos
Tres **tarjetas de opción** (radio buttons ocultos con `label` estilizado), una al lado de la otra en desktop y apiladas en móvil:

| Opción | Ícono | Texto secundario |
|---|---|---|
| Tarjeta de crédito / débito | `bi-credit-card-2-front` | Visa, Mastercard |
| Pago con QR | `bi-qr-code` | Escanea desde tu app bancaria |
| Transferencia bancaria | `bi-bank` | Datos de cuenta de ejemplo |

- Opción seleccionada: borde `--tas-green-light`, fondo ligeramente más claro y un check en la esquina.
- Al cambiar de opción, el panel inferior cambia con fade suave.

### Panel "Tarjeta"
Layout en desktop: **vista previa de la tarjeta** a la izquierda y **formulario** a la derecha (en móvil, la tarjeta arriba).

**Vista previa (`_TarjetaPreview.cshtml`)**
- Proporción de tarjeta real (`aspect-ratio: 1.586`), ancho máx. 340px, radio 16px.
- Fondo: degradado oscuro de verde bosque a casi negro (`#1E3A2B → #0B100E`) con un patrón SVG sutil de **curvas de nivel topográficas** en baja opacidad (evoca mapas de montaña).
- Contenido: pequeño texto "TREN AL SUR" arriba a la izquierda, un chip dibujado con CSS, número con formato `•••• •••• •••• ••••`, nombre del titular y vencimiento `MM/AA`, y la marca detectada como texto arriba a la derecha.
- Los datos se actualizan **en vivo** mientras el usuario escribe.
- Al enfocar el campo CVV, la tarjeta **gira** (`transform: rotateY(180deg)`, 0.6s ease) y muestra el reverso con la banda y el CVV. Al salir del campo, vuelve.
- Nada de brillos, reflejos animados ni colores neón.

**Formulario de tarjeta**
Campos: Número de tarjeta, Nombre del titular, Vencimiento (MM/AA), CVV.

> ⚠️ **IMPORTANTE DE SEGURIDAD:** estos cuatro inputs **NO llevan atributo `name`**.
> Así el navegador nunca los envía al servidor. Además usar `autocomplete="off"`.
> Solo se envían dos campos ocultos: `TarjetaMarca` y `TarjetaUltimos4`.

Comportamiento en `checkout.js`:
- **Máscara del número**: solo dígitos, espacio cada 4, máximo 16 dígitos (19 caracteres).
- **Detección de marca** por prefijo: empieza con `4` → VISA; `51–55` o `2221–2720` → MASTERCARD; otro → sin marca.
- **Validación Luhn** del número (función corta y comentada, porque es un proyecto para estudiar).
- **Vencimiento**: formato `MM/AA`, mes 01–12 y fecha no pasada.
- **CVV**: 3 dígitos (`inputmode="numeric"`, `type="password"` opcional).
- **Nombre**: se muestra en mayúsculas en la vista previa.
- Mensajes de error claros en español debajo de cada campo.
- Mostrar una ayuda discreta: *"Tarjeta de prueba: 4242 4242 4242 4242 · cualquier fecha futura · CVV 123"*.

### Panel "QR"
- Tarjeta centrada con un **QR de ejemplo** (imagen genérica o generado con un SVG de muestra; no debe corresponder a ninguna cuenta real).
- Texto: monto a pagar, *"Escanea el código desde la app de tu banco"* y la etiqueta visible **"QR de demostración"**.
- Un checkbox: *"Ya realicé el pago"* que habilita el botón de confirmar.

### Panel "Transferencia"
- Datos de cuenta **claramente ficticios** (Banco: "Banco de ejemplo", Cuenta: "000-0000000-0", Titular: "Tren al Sur (demo)") con botón para copiar cada dato.
- Mismo checkbox *"Ya realicé la transferencia"*.

### Botón final
- Texto dinámico: **"Pagar Bs 450,00"** (usar el formato de moneda del proyecto).
- Deshabilitado hasta que el método elegido esté válido.
- Debajo: 🔒 *"Tus datos están protegidos"* + *"Modo demostración — no se realizan cargos reales."*

---

## FASE 9 — Procesamiento y confirmación

### Overlay "Procesando pago"
Al enviar el formulario:
1. Deshabilitar el botón (evita doble clic).
2. Mostrar un overlay a pantalla completa con fondo `rgba(11,16,14,.92)`:
   - Un ícono de brújula (`bi-compass`) girando lentamente (3s por vuelta).
   - Texto: *"Procesando tu pago…"* y debajo, en gris, *"Preparando tu equipo para la ruta."*
3. Enviar el formulario normalmente (POST). El `Task.Delay` del servicio da la sensación de espera.

### Página `Confirmacion.cshtml`
Es el cierre emocional de la compra: **"En camino"**.

```
┌────────────────────────────────────────────────┐
│  Fondo: foto amplia de sendero / montaña al     │
│  amanecer, oscurecida con degradado             │
│                                                 │
│        ✓ (círculo verde con animación de trazo) │
│        ¡TODO LISTO PARA TU PRÓXIMA AVENTURA!    │
│        Tu pedido #123 fue registrado.           │
│        Código: TAS-20260926-4821                │
│                                                 │
│  ┌── Resumen del pedido ─────────────────────┐  │
│  │ productos · método (VISA •••• 4242)       │  │
│  │ total · datos de entrega                  │  │
│  └───────────────────────────────────────────┘  │
│                                                 │
│  [Ver mis pedidos]   [Explorar rutas]           │
└────────────────────────────────────────────────┘
```

- El check se dibuja con animación SVG (`stroke-dashoffset`, 0.8s).
- El botón **"Explorar rutas"** conecta con la sección Rutas/Servicios (hilo Productos → Rutas).
- **"Ver mis pedidos"** usa la vista de pedidos que ya existe.
- El estado mostrado es el estado real del pedido (el inicial del sistema), no "Pagado" inventado.

---

## FASE 10 — Responsive

| Pantalla | Comportamiento |
|---|---|
| Desktop (≥992px) | Dos columnas; resumen lateral `position: sticky; top: 100px`. |
| Tablet (768–991px) | Una columna; resumen arriba del formulario, colapsable. |
| Móvil (<768px) | Una columna; banda superior más baja (~120px); stepper solo con números; resumen como acordeón "Ver resumen (3) — Bs 450,00"; métodos de pago apilados; **botón de pago fijo abajo** (`position: sticky; bottom: 0`) con el total. |

- Áreas táctiles de mínimo 48px.
- Teclado numérico en móvil para tarjeta, vencimiento, CVV y teléfono (`inputmode="numeric"` / `tel`).
- Nada de scroll horizontal.

---

## FASE 11 — Animaciones (sutiles)

- Aparición de secciones: `opacity 0 → 1` + `translateY(12px → 0)`, 0.4s ease-out.
- Hover en opciones de pago: elevación de 2px y borde más claro, 0.2s.
- Giro de la tarjeta al enfocar CVV: 0.6s.
- Brújula del overlay: rotación lenta.
- Check de confirmación: trazo animado.
- Respetar `@media (prefers-reduced-motion: reduce)` desactivando animaciones.

**Evitar:** neón, brillos, rebotes, confeti, animaciones rápidas.

---

## FASE 12 — Seguridad (revisar antes de terminar)

- [ ] Los inputs de tarjeta **no tienen `name`** y no llegan al servidor (verificar en DevTools → Network → Payload).
- [ ] Solo se guardan/usan `TarjetaMarca` y `TarjetaUltimos4` (validados en servidor: 4 dígitos o nulo).
- [ ] Los totales se calculan **siempre en el servidor** desde la base de datos.
- [ ] `[ValidateAntiForgeryToken]` en el POST.
- [ ] La confirmación solo muestra pedidos del usuario actual.
- [ ] Carrito vacío → redirige al carrito, no permite checkout.
- [ ] No se escriben datos de tarjeta en logs ni en `console.log`.
- [ ] El texto "Modo demostración" es visible en el checkout.

---

## FASE 13 — Pruebas manuales

1. Checkout con carrito vacío → redirige al carrito.
2. Dejar campos de entrega vacíos → errores claros, no avanza.
3. Tarjeta `4242 4242 4242 4242` → marca VISA, Luhn válido, paga.
4. Tarjeta `1234 5678 9012 3456` → error "Número de tarjeta no válido".
5. Vencimiento pasado → error.
6. Enfocar CVV → la tarjeta gira.
7. Pago por QR y por transferencia con el checkbox → pedido creado.
8. Recargar la página de confirmación → no se duplica el pedido.
9. Intentar ver la confirmación de otro usuario cambiando el id → `404`.
10. Revisar en móvil (DevTools, 375px) y tablet (768px).
11. Confirmar en la BD que el pedido nace con el estado inicial y **no** se cuenta como ingreso en reportes.

---

## FASE 14 — Documentación

Agregar una sección al archivo `Agente/GUIA_DE_ESTUDIO.md` explicando, en lenguaje sencillo:
- El flujo completo Carrito → Checkout → Pago simulado → Pedido → Confirmación.
- Por qué existe `IPagoService` (poder cambiar a una pasarela real sin tocar el controlador).
- Por qué los datos de la tarjeta nunca llegan al servidor.
- Qué hace el algoritmo de Luhn.
- Qué es el patrón Post-Redirect-Get.

---

## Resumen para Claude Code

1. Analiza primero, **reutiliza** carrito y pedidos existentes.
2. El pago es **simulado**: ningún dato de tarjeta sale del navegador.
3. El servidor recalcula todo; el formulario solo aporta datos de entrega y método.
4. Diseño oscuro, sobrio, outdoor: **"Prepárate para salir"**.
5. Código simple y comentado: este proyecto se va a estudiar.
6. Trabaja por fases y espera confirmación entre cada una.
