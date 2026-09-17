# Decisiones — Tren al Sur

## 2026-09-15 — Decisiones iniciales confirmadas por el usuario

### Decisión
- Lenguaje principal: **C#**.
- Framework: **ASP.NET Core**.
- Arquitectura: **MVC**.
- Base de datos prevista: **PostgreSQL mediante Supabase**.

### Motivo
Decisiones establecidas por el usuario al iniciar el desarrollo real del proyecto (auditoría inicial).

### Consecuencia
- Todo el desarrollo futuro debe respetar este stack.
- No hay decisiones anteriores contradictorias registradas; estas son las decisiones vigentes.

---

## 2026-09-15 — Evolucionar el código existente (no reconstruir)

### Decisión
El proyecto parte de código existente (hoy identificado como "EcommerceApp") y **no debe reconstruirse desde cero**.

### Motivo
Existe desarrollo previo funcional que debe conservarse y sobre el cual se construirá.

### Consecuencia
- Las auditorías y cambios futuros deben comprender primero el sistema existente.
- Cualquier propuesta de reestructuración debe plantearse como cambio independiente.

---

## 2026-09-15 — Convención de código del proyecto (login y en adelante)

### Decisión
Prefencia confirmada por el usuario: **código sencillo, simple de entender**, con **comentarios breves sobre funciones**, en español, y con **estilo similar a los códigos de ejemplo del docente**.

### Motivo
Proyecto universitario por fases; el código debe ser fácil de explicar y revisar.

### Consecuencia
- Todo código nuevo (incluido el login social) debe seguir este estilo.
- Evitar abstracciones, patrones o dependencias innecesarias.

---

## 2026-09-15 — Login social: implementado en código, pendiente de credenciales

### Decisión (estado verificado)
El login social con Google y GitHub está **implementado en el código** (Program.cs, AccountController, vistas, modelo ExternalRegisterViewModel) y compila sin errores.

### Motivo
Fase universitaria de autenticación: se requiere "iniciar sesión/registrarse con Gmail, Google y GitHub" (Gmail se cubre con Google OAuth, confirmado por el usuario).

### Consecuencia
- **No es end-to-end funcional todavía**: falta cargar las credenciales OAuth reales (ClientId/ClientSecret) de Google Cloud y GitHub.
- Sin credenciales, los botones muestran un mensaje amigable y la app sigue funcionando (providers condicionales).
- **No se tocó** la zona del docente (arquitectura, BD, Supabase, ConnectionStrings).

---

## 2026-09-15 — Login social: credenciales cargadas y flujo HTTP verificado

### Decisión (estado verificado)
El usuario cargó las credenciales reales de Google y GitHub en `appsettings.json` → `Authentication`.

### Consecuencia
- Verificado vía HTTP: `POST /Account/ExternalLogin` responde 302 hacia Google (`accounts.google.com`, client_id propio, PKCE, `/signin-google`) y hacia GitHub (`github.com/login/oauth/authorize`, client_id propio, scope `user:email`, `/signin-github`).
- Pendiente: prueba end-to-end en navegador con cuenta real (depende del usuario). Las URIs de callback registradas usan `localhost:5187` (perfil `http`).

---

## 2026-09-15 — Restricción del docente: zonas intocables

### Decisión (restricción confirmada por el usuario)
Es un proyecto universitario desarrollado por fases. **NO se tocan por nada del mundo**:
- La arquitectura del proyecto.
- Supabase y la base de datos en general.
- Los `.json` con el connection string (sección `ConnectionStrings`) y el string en sí.

### Motivo
Son secciones y código definidos por el docente.

### Consecuencia
- Todo el desarrollo (incluido el login social) debe apoyarse en lo existente sin modificar esas zonas.
- En caso de ambigüedad sobre si algo pertenece a una zona intocable, preguntar antes de actuar.

---

## 2026-09-15 — Estado verificado: PostgreSQL/Supabase ya implementado

### Decisión (verificación de código, no decisión nueva)

El código ya usa **Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0** con connection string apuntando a Supabase:
- `Host=aws-0-us-east-2.pooler.supabase.com;Port=5432;Database=postgres;...SSL Mode=Require`.
- La migración `20260909154753_InitialCreate` es de proveedor Npgsql y las tablas ya existen en la BD remota (verificado ejecutando la app y consultando `Products`).

### Consecuencia
- **NO es necesaria una migración de proveedor**: el proyecto ya está sobre PostgreSQL/Supabase.
- Lo pendiente es configurar las credenciales de forma segura (hoy están en texto plano en `appsettings.json`) y decidir el esquema definitivo de la tienda.
- La tarea "migrar a PostgreSQL/Supabase" mencionada en el briefing inicial queda descartada por código verificado; se registra aquí para evitar trabajos innecesarios.

---

## 2026-09-17 — Ampliación del modelo de datos (AddProductFields, ExtendServiceModel)

### Decisión (aprobada por el usuario)
Se amplió el esquema con dos migraciones adicionales aplicadas en Supabase:
- `AddProductFields`: nuevos campos en `Product` — `Brand`, `IsNew`, `IsBestSeller`, `SecondaryImageUrl`.
- `ExtendServiceModel`: nuevos campos en `Service` — `ShortDescription`, `Region`, `StartPoint`, `EndPoint`, `DifficultyDescription`, `DurationHours`, `MaxGroupSize`, `PriceDescription`, `GalleryImages`, `Includes`, `Excludes`, `Recommendations`, coordenadas GPS (`StartLatitude`, `StartLongitude`, `EndLatitude`, `EndLongitude`), y relaciones `GuideId`/`TransportId`.

### Motivo
El modelo de datos de la tienda se amplió para cubrir las necesidades de la plataforma de senderismo: rutas con información completa (ubicación, duración, dificultad, qué incluye, galería de imágenes, recomendaciones) y productos con marca, estado de novedad, bestseller y segunda imagen.

### Consecuencia
- Las vistas de tienda (`ProductsController.Index`, `ServicesController.Index`, `Details`) se adaptaron a los nuevos campos.
- Los seeders (`ProductSeeder`, `ServiceSeeder`) se actualizaron con datos de ejemplo que usan los nuevos campos.
- El panel de administración (`AdminController`) permite crear/editar con los nuevos campos.
- Los filtros de productos ahora incluyen marca y búsqueda por marca.
- Los filtros de rutas incluyen región, dificultad, categoría, duración máxima y búsqueda.

---

## 2026-09-17 — Seeders automáticos de datos

### Decisión (aprobada por el usuario)
Se crearon `ProductSeeder` y `ServiceSeeder` como clases estáticas que se ejecutan al arrancar (`Program.cs`), sembrando datos de ejemplo si las tablas están vacías.

### Motivo
Permitir probar la app sin necesidad de inserts manuales en la BD. Los seeders contienen datos realistas de productos de senderismo y rutas de trekking.

### Consecuencia
- Al arrancar, si `Products` está vacía, se insertan 22 productos de ejemplo (tiendas de campaña, calzado, mochilas, bastones, ropa outdoor, camping, hidratación, iluminación, accesorios, seguridad y orientación).
- Al arrancar, si `Services` está vacía, se insertan 8 rutas detalladas (Parque Nacional Tunari, Laguna Angostura, Circuito Pairumani, Choro Trek, Salar de Uyuni, Machu Picchu Camino Inca, Torres del Paine W Circuit, Kilimanjaro Ruta Machame).
- Los datos son de ejemplo y deben reemplazarse antes de cualquier entrega.

---

## 2026-09-16 — Esquema de tienda: tablas Services, Reservations, Guides, Transports

### Decisión (aprobada por el usuario)
Se amplía el esquema con **4 tablas nuevas** (migración `AddBusinessEntities`, aditiva y reversible, aplicada en Supabase):
- `Services` (servicios/rutas: categoría, nombre, descripción, ubicación, dificultad, distancia, duración, precio, estado, imagen).
- `Reservations` con **clave compuesta `(UserId, ServiceId)`** y estados Pendiente/Recorrido/Acabado/Cancelado.
- `Guides` y `Transports` (definición inicial del usuario, propuesta agnóstica: sin FK a Services por ahora).

### Motivo
El usuario definió el modelo de datos de la tienda y autorizó explícitamente la migración en Supabase (zona del docente, excepción puntual aprobada).

### Consecuencia
- Nuevo esquema aplicado: tablas `Services`, `Reservations`, `Guides`, `Transports` + registros en `__EFMigrationsHistory`.
- `Products` (tienda) coexiste con `Services` (rutas). Pendiente: CRUD de ambas en el panel Admin y decidir si el catálogo de la tienda muestra también servicios.
- **Limitación conocida:** la clave compuesta `(UserId, ServiceId)` permite una única reserva por usuario-ruta. Si se requieren reservas múltiples de la misma ruta, cambiar el PK a identity + índice (UserId, ServiceId, BookingDate).
- Guías y transportes no están vinculados aún a reservas; se puede agregar la relación en una fase futura.