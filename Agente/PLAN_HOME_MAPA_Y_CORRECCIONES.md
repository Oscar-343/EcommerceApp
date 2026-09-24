# Plan: Hero con video fijo, mapa en el Home y correcciones — Tren al Sur

> **Para Claude Code.** Lee este archivo completo antes de empezar.
> El proyecto es universitario y el dueño lo va a **estudiar después**. Manda la simplicidad sobre la elegancia.

## Objetivo

1. Que el video del hero del Home se quede fijo al hacer scroll y la página lo vaya tapando (mismo efecto que el hero de Productos).
2. Reemplazar la sección **"EXPLORA A TU MANERA"** del Home por el mapa de rutas.
3. Permitir cargar coordenadas de las rutas desde el panel admin (hoy no se puede).
4. Corregir los errores detectados en la revisión del código.

## Decisiones ya tomadas (no las cambies sin preguntarme)

1. **No se agregan atributos nuevos a `Service`.** Ya existen `StartPoint`, `EndPoint`, `StartLatitude`, `StartLongitude`, `EndLatitude` y `EndLongitude`. Solo faltan en los formularios del admin.
2. **Mapa del Home:** muestra **todas** las rutas con coordenadas de inicio, pero el encuadre inicial es **Bolivia**. Las rutas de otros países aparecen al alejar el zoom.
3. **Un marcador por ruta** en el Home (solo el punto de inicio). El inicio y el fin se siguen mostrando solo en el detalle de la ruta.
4. **No se dibuja el recorrido del sendero** (no hay GPX ni trazado guardado).
5. **La navegación por categorías se quita del Home**, pero sigue existiendo en la página de Rutas (`Services/Index`).

## Reglas (valen en todas las fases)

1. **Código simple.** Sin paquetes NuGet ni librerías JS nuevas. Comentarios breves en español solo donde expliquen el *por qué*.
2. Respeta `Agente/AGENTS.md` y `Agente/.agent/*.md`. Si este plan los contradice, avísame antes de actuar.
3. **No inventes funcionalidades** fuera de este plan. Si algo requiere una decisión de diseño, pregúntame.
4. **No toques:** cadenas de conexión, `appsettings*.json`, valores de Supabase/Google/GitHub ni el framework. Nunca imprimas secretos.
5. **Este plan no necesita migraciones.** Si después de un cambio `dotnet ef migrations add` detectara diferencias en el modelo, **detente y avísame**. Nunca ejecutes `dotnet ef database update`.
6. **Excepción a la regla de `DataAnnotations`:** en la Fase 5 se quita el `RegularExpression` de `Product.Name` y `Service.Name`, y en la Fase 2 se agregan `[Range]` a las coordenadas. Ningún otro `DataAnnotation` se toca.
7. No borres archivos sin mostrarme antes la lista y esperar mi "sí".
8. Rama `home-mapa`. **Un commit por fase. No hagas push.**
9. Después de cada fase: `dotnet build` sin errores y dime qué probar a mano.

---

## Fase 1 — Video fijo en el hero del Home

**Problema:** `.hero-brand__media` usa `position: sticky`, pero mide lo mismo que su padre (`100vh`), así que no tiene recorrido y sube con el scroll. `position: fixed` sí lo deja quieto, pero `overflow: hidden` no recorta elementos `fixed` y el video se ve detrás de toda la página.

**Solución:** `position: fixed` en el video + `clip-path: inset(0)` en el hero (que sí recorta a los hijos `fixed`). El overlay sale del contenedor del video para que su degradado inferior suba con la sección y el video "se disuelva" en el borde, igual que en Productos.

### `Views/Home/Index.cshtml`

Mueve `<div class="hero-brand__overlay"></div>` **fuera** de `.hero-brand__media`, justo después de cerrarlo:

```html
<section class="hero-brand">

    <div class="hero-brand__media">
        <video class="hero-brand__video" ...> ... </video>
        <svg class="hero-brand__contours" ...> ... </svg>
    </div>

    <!-- Fuera del video: el degradado se desplaza con la sección -->
    <div class="hero-brand__overlay"></div>

    <!-- resto sin cambios: flourish, scrollcue, content -->
</section>
```

### `wwwroot/css/home.css`

Deja estas tres reglas así (el resto de `.hero-brand__*` no cambia):

```css
.hero-brand {
    position: relative;
    width: 100%;
    height: 100vh;
    display: grid;
    margin: 0;
    padding: 0;
    z-index: 0;
    /* Recorta el video fijo: solo se ve dentro del hero */
    clip-path: inset(0);
}

.hero-brand__media {
    /* Fijo a la ventana: el video no se mueve al hacer scroll */
    position: fixed;
    inset: 0;
    width: 100%;
    height: 100vh;
    z-index: 0;
    overflow: hidden;
    pointer-events: none;
}

.hero-brand__overlay {
    position: absolute;
    inset: 0;
    z-index: 1;
    pointer-events: none;
    background:
        linear-gradient(90deg, rgba(11,16,14,.95) 0%, rgba(11,16,14,.80) 32%, rgba(11,16,14,.40) 62%, rgba(11,16,14,.12) 100%),
        linear-gradient(180deg, rgba(11,16,14,.25) 0%, rgba(11,16,14,.35) 70%, rgba(11,16,14,.95) 100%);
}
```

- Quita de `.hero-brand__media` las propiedades `grid-area` y `top` (ya no aplican).
- `.hero-brand__stats` **no se toca**: su fondo opaco es el que tapa el video al subir.

**Probar:** en el Home, al hacer scroll el video queda quieto y la franja de estadísticas lo cubre. El video no debe verse detrás de ninguna otra sección.

---

## Fase 2 — Coordenadas en el formulario de rutas (admin)

### `Models/Service.cs`

Agrega solo los `[Range]` a las cuatro coordenadas existentes (no cambia el esquema de la BD):

```csharp
// === GEOGRAFÍA (para mapa) ===
[Range(-90, 90, ErrorMessage = "La latitud debe estar entre -90 y 90.")]
public double? StartLatitude { get; set; }

[Range(-180, 180, ErrorMessage = "La longitud debe estar entre -180 y 180.")]
public double? StartLongitude { get; set; }

[Range(-90, 90, ErrorMessage = "La latitud debe estar entre -90 y 90.")]
public double? EndLatitude { get; set; }

[Range(-180, 180, ErrorMessage = "La longitud debe estar entre -180 y 180.")]
public double? EndLongitude { get; set; }
```

### `Views/Admin/ServiceCreate.cshtml` y `Views/Admin/ServiceEdit.cshtml`

Justo después de la sección que contiene "Punto de inicio" y "Punto final", agrega una sección nueva con las mismas clases que ya usa el formulario:

```html
<div class="admin-form-section">
    <h3>Coordenadas del mapa</h3>
    <p class="hint">Opcional. Cópialas de Google Maps (clic derecho sobre el punto). Ejemplo: -17.3935, -66.1570</p>
    <div class="form-grid">
        <div class="form-field">
            <label asp-for="StartLatitude">Latitud de inicio</label>
            <input asp-for="StartLatitude" type="number" step="any" class="form-control" />
            <span asp-validation-for="StartLatitude" class="text-danger"></span>
        </div>
        <div class="form-field">
            <label asp-for="StartLongitude">Longitud de inicio</label>
            <input asp-for="StartLongitude" type="number" step="any" class="form-control" />
            <span asp-validation-for="StartLongitude" class="text-danger"></span>
        </div>
        <div class="form-field">
            <label asp-for="EndLatitude">Latitud final</label>
            <input asp-for="EndLatitude" type="number" step="any" class="form-control" />
            <span asp-validation-for="EndLatitude" class="text-danger"></span>
        </div>
        <div class="form-field">
            <label asp-for="EndLongitude">Longitud final</label>
            <input asp-for="EndLongitude" type="number" step="any" class="form-control" />
            <span asp-validation-for="EndLongitude" class="text-danger"></span>
        </div>
    </div>
</div>
```

- Los controladores `ServiceCreate`/`ServiceEdit` ya reciben el `Service` completo: **no hace falta cambiarlos**.
- **Verifica el separador decimal:** guarda una ruta con `-17.3935` y confirma que en la BD queda `-17.3935` (no `-173935` ni vacío). Pruébalo también al volver a abrir "Editar". Si falla, **detente y avísame** antes de cambiar la configuración de cultura.

---

## Fase 3 — Quitar "EXPLORA A TU MANERA" del Home

1. `Views/Home/Index.cshtml`: elimina la sección completa `<section class="section-explore-way">…</section>` (incluye el diccionario `categoryImages`).
2. `wwwroot/css/home.css`: elimina las reglas que solo usa esa sección (`.section-explore-way`, `.categories-grid-outdoor`, `.category-explore-card`, `.category-image`, `.category-overlay`, `.category-name`, `.category-explore-text` y sus `@media`). **Antes de borrar cada clase, busca con grep que no se use en otra vista.**
3. `Models/HomeViewModel.cs`: elimina la propiedad `OutdoorCategories`.
4. `Controllers/HomeController.cs`: elimina la variable `outdoorCategories` y su asignación en el modelo.
5. Revisa `wwwroot/js/home.js` por si anima esas tarjetas; si es así, quita solo esa parte.

---

## Fase 4 — Mapa de rutas en el Home

### `Controllers/HomeController.cs`

Reemplaza la consulta de `routeMarkers`: solo rutas activas **con coordenadas de inicio**, sin límite de cantidad.

```csharp
// Marcadores del mapa: solo rutas que tienen coordenadas de inicio.
var routeMarkers = await _context.Services.AsNoTracking()
    .Where(s => s.Status == "Active" && s.StartLatitude.HasValue && s.StartLongitude.HasValue)
    .Select(s => new MapMarker
    {
        Id = s.Id,
        Name = s.Name,
        Location = s.Location,
        Difficulty = s.Difficulty,
        DurationHours = s.DurationHours,
        Latitude = s.StartLatitude,
        Longitude = s.StartLongitude
    })
    .ToListAsync();
```

### `Views/Home/Index.cshtml`

En la sección `rutas-map-section`:

1. Corrige el comentario: el script es `rutas-map.js`, no `servicios.js`.
2. Agrega `data-initial-view="bolivia"` al contenedor.
3. Envía también dificultad (en texto) y duración:

```cshtml
@{
    // La consulta ya filtra rutas sin coordenadas; aquí solo se arma el JSON del mapa.
    var homeMapRoutes = Model.RouteMarkers.Select(r => new
    {
        id = r.Id,
        name = r.Name,
        location = r.Location,
        difficulty = r.Difficulty != null && RouteDifficulty.Labels.ContainsKey(r.Difficulty)
            ? RouteDifficulty.Labels[r.Difficulty] : null,
        duration = r.DurationHours,
        lat = r.Latitude,
        lng = r.Longitude
    }).ToList();
}
@if (homeMapRoutes.Any())
{
    <section class="rutas-map-section">
        <div class="container-fluid">
            <div class="section-header">
                <h2 class="section-title">RUTAS CERCA DE TI</h2>
                <p class="section-subtitle">Descubre en el mapa las rutas disponibles.</p>
            </div>
            <div id="rutasMap" class="rutas-map"
                 data-initial-view="bolivia"
                 data-routes='@Html.Raw(System.Text.Json.JsonSerializer.Serialize(homeMapRoutes))'></div>
        </div>
    </section>
}
```

- **Título y subtítulo:** déjalos como están. Pregúntame el texto nuevo antes de cambiarlo (el mapa no usa la ubicación del usuario, así que "cerca de ti" se va a reemplazar).
- `JsonSerializer` ya escapa `'`, `<` y `>`, así que `Html.Raw` dentro del atributo es seguro.

### `wwwroot/js/rutas-map.js`

Este archivo lo usan **Home, Services/Index y Services/Details**. Los dos cambios deben mantener funcionando las otras dos páginas.

**a) Encuadre configurable.** Reemplaza el bloque final (`if (bounds.length === 1) … else fitBounds`) por:

```javascript
// Límites aproximados de Bolivia (suroeste, noreste).
// Se usan límites y no un zoom fijo para que se adapte al tamaño de pantalla.
const BOLIVIA_BOUNDS = [[-22.9, -69.7], [-9.6, -57.4]];

if (el.dataset.initialView === 'bolivia') {
    map.fitBounds(BOLIVIA_BOUNDS, { padding: [20, 20] });
} else if (bounds.length === 1) {
    map.setView(bounds[0], 8);
} else {
    map.fitBounds(bounds, { padding: [40, 40] });
}
```

**b) Popup seguro.** Hoy el popup concatena `route.name` dentro del HTML; si un nombre tuviera `<`, se interpretaría como HTML. Constrúyelo con elementos y `textContent`:

```javascript
// Arma el popup con textContent para que ningún texto se interprete como HTML.
function crearPopup(route) {
    const box = document.createElement('div');
    box.className = 'rutas-map-popup';

    const titulo = document.createElement('strong');
    titulo.textContent = route.name;
    box.appendChild(titulo);

    // Línea de detalle: ubicación · dificultad · duración (solo las que existan)
    const detalles = [route.location, route.difficulty, route.duration ? route.duration + ' h' : null]
        .filter(Boolean);
    if (detalles.length) {
        const p = document.createElement('div');
        p.textContent = detalles.join(' · ');
        box.appendChild(p);
    }

    if (route.id) {
        const link = document.createElement('a');
        link.href = '/Services/Details/' + encodeURIComponent(route.id);
        link.textContent = 'Ver ruta →';
        box.appendChild(link);
    }

    return box;
}
```

Y en el `forEach`: `marker.bindPopup(crearPopup(route));`

- Services/Index y Services/Details no envían `difficulty` ni `duration`: el `filter(Boolean)` los omite sin errores.

**Probar:** el Home abre encuadrado en Bolivia en escritorio y en móvil; al alejar aparecen las rutas de otros países; el popup muestra nombre, detalle y enlace. Services/Index y Services/Details siguen encuadrando igual que antes.

---

## Fase 5 — Correcciones de la revisión

### 5.1 Nombres con acentos no pasan la validación del navegador

`Product.Name` y `Service.Name` tienen `RegularExpression(@"^[\w\s\-]+$")`. En JavaScript `\w` solo acepta ASCII, así que el producto sembrado **"Pantalón Trekking Resistente"** no se puede guardar desde el admin. Razor ya escapa el HTML al mostrar, por lo que el regex no aporta seguridad.

- En `Models/Product.cs` y `Models/Service.cs`: quita **solo** el `RegularExpression` de `Name`. Deja `Required` y `MaxLength`.
- No toques los `RegularExpression` de `Difficulty` ni de los estados.

### 5.2 `GuideEdit` y `TransportEdit` pisan `CreatedAt`

Mismo arreglo que ya tiene `ProductEdit`. Después de `Update(...)`:

```csharp
context.Guides.Update(guide);
// Update() marca todo como modificado; CreatedAt no debe pisarse con el valor del formulario.
context.Entry(guide).Property(g => g.CreatedAt).IsModified = false;
```

Igual en `AdminController.Transports.cs` con `transport`. Luego quita esta tarea de `Agente/.agent/TASKS.md`.

### 5.3 Subir imágenes solo si el formulario es válido

En `ProductCreate`, `ProductEdit`, `ServiceCreate` y `ServiceEdit` las imágenes se suben a Supabase **antes** de revisar `ModelState`. Si hay un error de validación, los archivos quedan huérfanos en el bucket. Mueve la validación al inicio del `try`:

```csharp
// Validar primero: si el formulario tiene errores, no se sube nada a Supabase.
if (!ModelState.IsValid)
    return View(product);

// ...recién aquí las subidas de imagen, secundaria y galería...
```

### 5.4 Acción `Branding` sin vista

`AdminController.Branding()` no tiene vista (`Views/Admin/Branding.cshtml` no existe) y nada la enlaza. Elimina el método y corrige el comentario de `UploadImage` que menciona "Branding". No toques `UploadImage`.

---

## Fase 6 — Stock y cupo con compras simultáneas

**Problema:** el stock se lee, se valida y se escribe `product.Stock -= cantidad`. Si dos usuarios compran la última unidad a la vez, ambos pasan la validación. Lo mismo ocurre con el cupo de las reservas.

### 6.1 `OrdersController.Checkout`

Mantén la validación previa (da mensajes claros), pero **descuenta el stock con un update atómico** dentro de la transacción. Quita la línea `product.Stock -= item.Quantity;` del bucle que arma el pedido.

```csharp
await using var transaction = await context.Database.BeginTransactionAsync();

foreach (var item in items)
{
    // Descuenta solo si todavía hay stock suficiente. Es una sola operación en la BD,
    // así dos compras simultáneas no pueden vender la misma unidad.
    var filas = await context.Products
        .Where(p => p.Id == item.ProductId && p.Stock >= item.Quantity)
        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity));

    if (filas == 0)
    {
        await transaction.RollbackAsync();
        TempData["Success"] = $"\"{item.Product!.Name}\" ya no tiene stock suficiente. Ajusta la cantidad para continuar.";
        return RedirectToAction("Index", "Cart");
    }
}

// ...después se arma el pedido igual que ahora (sin tocar product.Stock),
// se agrega a context.Orders, se borra el carrito, SaveChangesAsync y CommitAsync.
```

### 6.2 Devolver stock al cancelar

En `OrdersController.Cancel` y en la cancelación de `AdminController.Orders.cs` (hoy hacen `product.Stock += item.Quantity`), usa también un update atómico dentro de una transacción:

```csharp
// Devuelve el stock directamente en la BD (suma sobre el valor actual, no sobre uno leído antes).
await context.Products
    .Where(p => p.Id == item.ProductId)
    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity));
```

Si `ProductId` es `null` (producto borrado), omite esa línea como ya se hace hoy.

### 6.3 `ReservationsController.Create`

Justo después de `BeginTransactionAsync()`, bloquea la fila de la ruta. Así, dos reservas simultáneas de la misma ruta se atienden una detrás de otra y el cálculo de cupo siempre ve el dato real:

```csharp
await using var transaction = await context.Database.BeginTransactionAsync();

// Bloquea la ruta hasta el commit: otra reserva de la misma ruta espera su turno.
await context.Database.ExecuteSqlInterpolatedAsync(
    $"SELECT 1 FROM \"Services\" WHERE \"Id\" = {serviceId} FOR UPDATE");

// ...el resto (duplicado, cupo, crear reserva) queda igual...
```

- No agregues validación de cupo a `AdminController.ReservationCreate` (no está en este plan).

**Probar:** una compra normal descuenta el stock; comprar más de lo disponible muestra el mensaje y el carrito queda intacto; cancelar devuelve el stock; una reserva normal se crea igual que antes.

---

## Fase 7 — Documentación

Actualiza `Agente/.agent/PROJECT_STATE.md` y `Agente/.agent/TASKS.md`:

- Hero del Home con video fijo (`fixed` + `clip-path`).
- Home sin "EXPLORA A TU MANERA"; mapa con encuadre inicial en Bolivia.
- Coordenadas editables desde el admin.
- Checkout, cancelación y reservas protegidos ante operaciones simultáneas.
- Corrige la frase "Sin AJAX en el catálogo": `CartController.Add` y `FavoritesController.Toggle` sí responden JSON cuando la petición es AJAX.
- Quita de `TASKS.md` el bug de `CreatedAt` ya resuelto.

---

## Fuera de alcance (no lo hagas en este plan)

- Separar la solución en proyectos MVC + API, crear DTOs o mover lógica a servicios nuevos.
- Rotar o mover credenciales (lo hace el dueño manualmente).
- Constantes para los estados (`"Pendiente"`, `"Active"`…), `TempData["Error"]` separado de `TempData["Success"]`, o quitar el selector de cantidad de la tarjeta de producto.
- Ubicación del usuario (geolocalización), trazado GPX o líneas entre inicio y fin en el Home.
- Cambiar el título de la sección del mapa sin preguntarme.

## Checklist final

- [ ] `dotnet build` sin errores ni advertencias nuevas.
- [ ] Ninguna migración nueva generada.
- [ ] Home: video fijo, sin categorías, mapa encuadrado en Bolivia.
- [ ] Services/Index y Services/Details: mapas funcionando como antes.
- [ ] Admin: se puede crear y editar una ruta con coordenadas decimales, y editar "Pantalón Trekking Resistente".
- [ ] Checkout, cancelación y reservas probados a mano.
- [ ] Un commit por fase en la rama `home-mapa`, sin push.
