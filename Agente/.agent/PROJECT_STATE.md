# Estado del Proyecto — Tren al Sur

Actualizado: 2026-09-17 (auditoría ampliada).

## Qué existe

- Solución `EcommerceApp.slnx` + proyecto web MVC único (`net10.0`).
- Autenticación con Identity (registro, login, logout, roles `Admin`/`User`).
- **Sistema dividido en dos:**
  - **Tienda pública:** `ProductsController` con catálogo completo (Index con filtros avanzados, Details) + **`ServicesController` público** (catálogo de rutas con filtros avanzados, Index, Details).
  - **Panel de administración (`[Authorize(Roles="Admin")]`):** `AdminController` con CRUD completo para Productos, Rutas/Servicios, Guías y Transportes + gestión de Reservas + sección de Branding (subida de imágenes/logo a Supabase Storage).
- **Seeder de datos:** `ProductSeeder.cs` y `ServiceSeeder.cs` con datos de ejemplo semilla automática (solo si la tabla está vacía).
- **Servicios inyectables:** `IImageStorageService` / `SupabaseImageStorageService` (subida de archivos a Supabase Storage) y `IEmailSender` / `SmtpEmailSender` (envío de correos por SMTP con fallback a consola).
- Páginas de plantilla sin personalizar: Home, Privacy, Error.
- **Migraciones EF Core aplicadas en Supabase:** `InitialCreate`, `AddBusinessEntities` (tablas Services, Reservations, Guides, Transports), `AddProductPromotion` (IsFeatured + PromotionalPrice en Products).
- **Modelo de datos expandido:**
  - `Product`: Id, Name, Description, Price, PromotionalPrice, IsFeatured, IsNew, IsBestSeller, Stock, ImageUrl, SecondaryImageUrl, Brand, Category, CreatedAt, UpdatedAt.
  - `Service`: Id, Name, Category, Description, ShortDescription, Region, Location, StartPoint, EndPoint, Difficulty, DifficultyDescription, DistanceKm, DurationHours, Duration, MaxGroupSize, Price, PriceDescription, ImageUrl, GalleryImages, SecondaryImageUrl, Status, IsFeatured, Includes, Excludes, Recommendations, StartLatitude/Longitude, EndLatitude/Longitude, GuideId/Guide, TransportId/Transport, CreatedAt, UpdatedAt.
  - `Reservation`: clave compuesta `(UserId, ServiceId)`, BookingDate, PeopleCount, UnitPrice, TotalPrice, Status (Pendiente/Recorrido/Acabado/Cancelado).
  - `Guide`: Id, Name, Specialty, Bio, Phone, Email, IsActive, CreatedAt, UpdatedAt.
  - `Transport`: Id, Name, Type, Capacity, Description, PricePerKm, IsActive, CreatedAt, UpdatedAt.
- **ViewModels:** `ProductosViewModel` (filtros de tienda), `ServiciosViewModel` (filtros de rutas), `ServicioDetalleViewModel` (vista detallada con galería, includes, excludes, recommendations parseados), `HomeViewModel`, `CatalogViewModel`.
- **Clases estáticas de utilidad en `Service.cs`:** `RouteCategories`, `RouteDifficulty` (con Labels/Colors), `RouteRegions`.
- **Datos de ejemplo semilla:**
  - **ProductSeeder:** 22 productos (tiendas de campaña, calzado de trekking, mochilas, bastones, ropa outdoor, camping, hidratación, iluminación, accesorios, seguridad y orientación).
  - **ServiceSeeder:** 8 rutas detalladas (Parque Nacional Tunari, Laguna Angostura, Circuito Pairumani, Choro Trek, Salar de Uyuni, Machu Picchu Camino Inca, Torres del Paine W Circuit, Kilimanjaro Ruta Machame).
- **Filtros dinámicos en tienda pública:**
  - Productos: filtro por categoría, búsqueda (nombre/descripción/marca), rango de precios (PromotionalPrice o Price), marca, ordenamiento (featured, precio asc/desc, más recientes). Endpoints AJAX `GetFilteredProducts` y `GetFilterOptions`.
  - Servicios: filtro por región, dificultad, categoría, duración máxima, búsqueda. Endpoint AJAX `GetFilterOptions`.
- **Galería de imágenes en rutas:** campo `GalleryImages` con URLs separadas por `|`, parseado en vista de detalles.
- **Incluye/Excluye/Recomendaciones:** campos de texto con pipe separado, parseados en vista detallada de rutas.

## Qué funciona (verificado)

- **Compilación OK**: 0 errores, 0 warnings (`dotnet build EcommerceApp.slnx`).
- **Arranque OK** en Development (`http://localhost:5187`): responde `/`, `/Account/Login`, `/Account/Register`, `/Products/Index` con HTTP 200.
- **Conexión a BD OK**: Supabase responde por TCP 5432; las consultas a todas las tablas se ejecutan sin error.
- **Roles `Admin`/`User` ya creados** en la BD (el arranque solo los verificó, no los insertó).
- **Seed de datos automático**: al arrancar, `ProductSeeder.SeedProductsAsync` y `ServiceSeeder.SeedServicesAsync` rellenan las tablas si están vacías (22 productos + 8 rutas).
- **Login/registro con email y contraseña**: funcional (Identity).
- **Login social (Google/GitHub)**: credenciales reales cargadas en `appsettings.json` → `Authentication`. Verificado vía HTTP: `ExternalLogin` redirige 302 a Google y GitHub. Los providers se registran condicionalmente (solo si hay credenciales), por lo que la app funciona sin ellas.
- **Formulario de registro externo (`ExternalRegister`)**: permite completar datos cuando GitHub no expone el email.
- **Recuperación de contraseña** implementada y verificada: flujo completo con token de Identity (`ForgotPassword` → `ForgotPasswordConfirmation` → `ResetPassword` → `ResetPasswordConfirmation`). Envío por SMTP configurable (`appsettings.json` → `Email`); sin SMTP imprime el enlace en la consola.
- **Subida de imágenes a Supabase Storage**: funcional desde el panel de administración. Validación de tipo (JPG/PNG/WEBP/GIF/MP4/WEBM) y tamaño (5 MB imágenes, 100 MB videos). Se usa en ProductCreate/Edit (imagen + secundaria), ServiceCreate/Edit (imagen + secundaria + galería) y Branding.
- **Gestión de branding**: vista `Admin/Branding` para subir logo, video de fondo e imágenes de marca a Supabase Storage.
- **Dashboard de administración** (`Admin/Index`): muestra contadores de productos, servicios, guías activos, transportes activos, reservas totales y productos con bajo stock (< 5). Lista de las 6 reservas más recientes con usuario y servicio. Badge de reservas pendientes en el sidebar (se actualiza en cada acción del admin vía `OnActionExecutionAsync`).
- **CRUD completo de Guías y Transportes** en el panel admin con: listado con conteo de rutas asignadas, creación, edición, eliminación suave (se marca `IsActive = false` si hay rutas asignadas en vez de eliminar).
- **Gestión de reservas**: listado filtrado por estado, con opción de actualizar el estado (`ReservationUpdateStatus`).
- **Filtros avanzados en tienda pública** (`ProductsController.Index`): búsqueda por nombre/descripción/marca, filtro por categoría, marca, rango de precios, ordenamiento múltiple. AJAX para carga dinámica de productos (`GetFilteredProducts`) y opciones de filtro (`GetFilterOptions`).
- **Filtros avanzados en catálogo de rutas** (`ServicesController.Index`): filtro por región, dificultad, categoría, duración máxima, búsqueda. Endpoint `GetFilterOptions` con categorías, regiones y niveles de dificultad. Vista de detalles con galería de imágenes, includes/excludes/recommendations parseados, dificultad con colores/emojis.
- **Redirección por rol (Fase 2, 2026-09-16)**: `RedirectForRole` en `AccountController` → Admin a `/Admin/Index`, resto a `/Products/Index`. Se aplica en los 5 puntos de login.
- **Navbar con roles (Fase 3, 2026-09-16)**: links "Tienda" (público) y "Panel de administración" (solo Admin).
- **Logo "Tren al Sur"** colocado en navbar y pantallas de auth.

## Qué NO funciona / problemas conocidos

- **Credenciales en texto plano**: `appsettings.json` contiene la cadena de conexión con contraseña de Supabase. Debe migrarse a user-secrets/entorno antes de exponer el código.
- **Imagen por defecto faltante**: las vistas de productos referencian `/images/default-product.jpg` y la carpeta `wwwroot/images/` no existe → imágenes rotas cuando un producto no tiene `ImageUrl`.
- **Warning de arranque**: `HttpsRedirectionMiddleware: Failed to determine the https port for redirect` al usar el perfil `http` (artefacto de dev, inofensivo).
- **Todos los datos son de ejemplo/semilla**: productos y rutas de prueba. Reemplazar por datos/imágenes reales antes de cualquier entrega.
- **Frontend de plantilla**: todo el branding sigue siendo "EcommerceApp" (navbar, título, footer); Home es la página Welcome de la plantilla.
- **La app arranca en Login** (ruta por defecto `Account/Login`): no hay landing pública aún (solo Products es público).
- **Login social no probado end-to-end** en navegador con cuenta real (solo verificado vía HTTP 302).
- **Guías/Transports no vinculados aún a Reservas** como FKs (se usa como catálogo de selección en el formulario de rutas).
- **`ExternalRegister` GET** devuelve datos del proveedor externo si están disponibles, pero el flujo puede requerir completar el nombre manualmente si el proveedor no expone el nombre completo.

## Estado de la persistencia

- Proveedor: **Npgsql** (PostgreSQL) + Supabase. NO hay MySQL (nunca estuvo).
- Connection string: en `appsettings.json`, clave `ConnectionStrings:postgresql`.
- Migraciones: tres (`20260909154753_InitialCreate`, `20260916153408_AddBusinessEntities`, `20260916192453_AddProductPromotion`, `20260917020746_AddProductFields`, `20260917024302_ExtendServiceModel`); el arranque NO ejecuta migraciones automáticas (`Database.Migrate` no está en Program.cs) → se aplican manualmente.
- Esquema creado en la BD remota: tablas de Identity + Products + Services + Reservations + Guides + Transports.

## Servicios y utilidades

- **`IImageStorageService` / `SupabaseImageStorageService`**: sube archivos al Storage de Supabase vía API REST. Devuelve URL pública. Configurado con `Supabase:Url`, `Supabase:ServiceRoleKey`, `Supabase:Bucket` en `appsettings.json`. Se usa desde `AdminController` para subir imágenes de productos, rutas y branding.
- **`IEmailSender` / `SmtpEmailSender`**: envía correos por SMTP usando sección `Email` de configuración. Si `Host` está vacío, imprime el correo en la consola (ideal para desarrollo). Se usa en `AccountController` para recuperación de contraseña.
- **`ProductSeeder`**: siembra 22 productos de ejemplo si `Products` está vacía.
- **`ServiceSeeder`**: siembra 8 rutas detalladas si `Services` está vacía.

## Integraciones pendientes

- Definir modelo de datos de tienda definitivo (carrito, órdenes, categorías, etc.).
- Personalización de identidad visual "Tren al Sur".
- Gestión segura de secretos/configuración.
- Git CLI no instalado en el PATH de esta máquina (el repo existe: remote GitHub `Oscar-343/EcommerceApp`, rama `master`, 2 commits).
- Vincular Guías/Transports a Reservas como relaciones FKs.

## Entorno

- SDK .NET instalados: 10.0.200 y 10.0.401. Runtimes ASP.NET Core 10.0.4 y 10.0.12.
- Perfiles de lanzamiento: `http` (5187) y `https` (7008).
- Frontend local: Bootstrap 5.3.3, jQuery 3.7.1, jQuery Validation 1.21.0, jQuery Unobtrusive Validation 4.0.0.
- Nuevas migraciones: `AddProductFields` (2026-09-17) y `ExtendServiceModel` (2026-09-17) con campos adicionales en Product y Service (Brand, IsNew, IsBestSeller, ShortDescription, Region, StartPoint, EndPoint, DifficultyDescription, DurationHours, MaxGroupSize, PriceDescription, GalleryImages, Includes, Excludes, Recommendations, coordenadas GPS).
