# Tareas — Tren al Sur

Actualizado: 2026-09-17.

## Pendientes (importantes)

- [ ] Login social: **prueba end-to-end desde navegador** con cuenta real de Google y GitHub (la hace el usuario con su sesión; el flujo HTTP ya está verificado). Recordar: usar el perfil `http` (puerto 5187) porque las URIs de callback registradas son `localhost:5187`; si se usa el perfil `https` (7008), hay que registrar esas URIs en los proveedores.
- [ ] Definir el alcance y las funcionalidades reales de la tienda "Tren al Sur" (productos/servicios de senderismo para Argentina): entidades, procesos y reglas de negocio.
- [ ] Definir el modelo de datos definitivo de la tienda y qué pasa con la entidad `Product` actual (¿se conserva, se extiende, se cataloga por categorías?).
- [ ] Decidir la identidad visual/nombre mostrado: hoy todo dice "EcommerceApp".
- [ ] Sacar las credenciales de base de datos de `appsettings.json` (user-secrets/entorno) antes de cualquier despliegue.
- [ ] Resolver la imagen por defecto de productos (`wwwroot/images/default-product.jpg` no existe).
- [ ] Decidir si la app debe arrancar en una landing pública (hoy arranca en Login) y qué requiere autenticación.
- [ ] Probar en navegador el panel con la cuenta Admin (`oscarrodrigorivassossa70@gmail.com` + su contraseña local).
- [ ] Vincular Guías/Transports a Reservas como relaciones FKs (decidir si se agrega FK `GuideId`/`TransportId` a `Reservations` o se mantiene como selección de catálogo).
- [ ] Definir si `Services` los categoriza con dropdown (hoy categoría libre en el form de admin de servicios) y si se agregan campos como duración/dificultad a la vista filtrada.
- [ ] Decidir si la tienda (catálogo) muestra también `Services` o solo `Products`.

## En progreso

- [ ] Auditoría técnica inicial y ampliación de documentación (esta etapa): inspección + documentación actualizada. En curso.

## Completadas relevantes

- [x] Auditoría de estructura, código, dependencias, configuración y Git.
- [x] Verificación de compilación (0 errores, 0 warnings).
- [x] Verificación de arranque y conexión a Supabase (tablas existentes, `Products` vacía).
- [x] Inicialización de la memoria persistente en `.agent/`.
- [x] Login social con Google y GitHub: código implementado (Program.cs, AccountController, vistas, modelo externo) y compilado. **Pendiente: credenciales reales + prueba end-to-end.**
- [x] Corregido login social roto (2026-09-15): `Login.cshtml` tenía los botones Google/GitHub anidados dentro del formulario principal; se separaron en formularios hermanos. Verificado vía HTTP: POST a `ExternalLogin` responde 302 a Google y GitHub.
- [x] "¿Olvidaste tu contraseña?" implementado (2026-09-16): flujo con token de Identity, vistas estilizadas y envío por SMTP configurable (sin SMTP → imprime enlace en consola). Verificado: GET/POST/confirmación responden correctamente. Pendiente E2E en navegador y configurar SMTP real si se desea.
- [x] Logo "Tren al Sur" colocado en navbar y pantallas de auth; queda reemplazar `src="/images/logo.png"` por la URL real (la hace el usuario).
- [x] Esquema de tienda creado y aplicado en Supabase (2026-09-16): tablas `Services`, `Reservations` (clave compuesta), `Guides`, `Transports` + entidades C# + configuración en DbContext. Migración `AddBusinessEntities` verificada (aditiva, 4 CreateTable, reversible).
- [x] **Fase 1 completada (2026-09-16)**: `AdminController` + `Views/Admin/` (dashboard, CRUD Productos y Servicios); `ProductsController` reducido a tienda read-only. Verificado: `/Admin/Index` → 302 a Login sin sesión; `/Products/Create` → 404; `/Products/Index` público 200.
- [x] **Cuenta admin habilitada (2026-09-16):** `oscarrodrigorivassossa70@gmail.com` (cuenta local con contraseña) promovida a rol `Admin` vía INSERT en `AspNetUserRoles` (verificado: `[Admin, User]`).
- [x] **Vistas huérfanas eliminadas (2026-09-16, con autorización):** `Views/Products/Create.cshtml`, `Edit.cshtml`, `Delete.cshtml`. Quedan solo `Index.cshtml` y `Details.cshtml` (tienda).
- [x] **Fase 2 completada (2026-09-16):** `RedirectForRole` en `AccountController` (Admin → `/Admin/Index`, resto → `/Products/Index`) aplicado en los 5 puntos de login + `Logout` → tienda. Corregido error CS0136 en `ExternalLoginCallback` (variable `user` renombrada a `existingUser`). Verificado compilación (0 errores) y runtime.
- [x] **Fase 3 completada (2026-09-16):** navbar con link "Tienda" (`Products/Index`, visible para todos) + "Panel de administración" (`Admin/Index`, visible solo con `User.IsInRole("Admin")`). Verificado build 0 errores y runtime.
- [x] **Sección de productos perfeccionada (2026-09-16):** portada con destacados, filtro `?category=...`, catálogo público de servicios, precios de oferta, panel admin con dropdown de categorías, marcado de destacado y precio de oferta. Seed de datos (12 productos + 5 rutas). Verificado en runtime.
- [x] **Ampliación del modelo de datos (2026-09-17):** migraciones `AddProductFields` y `ExtendServiceModel` con nuevos campos en Product (Brand, IsNew, IsBestSeller, SecondaryImageUrl) y Service (ShortDescription, Region, StartPoint, EndPoint, DifficultyDescription, DurationHours, MaxGroupSize, PriceDescription, GalleryImages, Includes, Excludes, Recommendations, coordenadas GPS, GuideId/TransportId).
- [x] **Seeders automáticos:** `ProductSeeder` (22 productos) y `ServiceSeeder` (8 rutas detalladas) con semilla automática al arrancar si las tablas están vacías.
- [x] **Filtros avanzados en tienda pública:** `ProductsController.Index` con filtros por categoría, marca, rango de precios, búsqueda, ordenamiento + AJAX (`GetFilteredProducts`, `GetFilterOptions`). `ServicesController.Index` con filtros por región, dificultad, categoría, duración + AJAX (`GetFilterOptions`).
- [x] **CRUD completo de Guías y Transportes en Admin** con conteo de rutas asignadas y eliminación suave (marca `IsActive = false` si hay rutas asignadas).
- [x] **Gestión de reservas en Admin:** listado filtrado por estado + actualización de estado (`ReservationUpdateStatus`).
- [x] **Sección de Branding en Admin:** subida de logo/video/imágenes de marca a Supabase Storage.
- [x] **Servicios inyectables:** `SupabaseImageStorageService` (subida de archivos a Supabase con validación de tipo/tamaño) y `SmtpEmailSender` (envío por SMTP con fallback a consola).
- [x] **ViewModels ampliados:** `ProductosViewModel`, `ServiciosViewModel`, `ServicioDetalleViewModel`, `HomeViewModel`, `CatalogViewModel`.
- [x] **Clases estáticas de utilidad en Service.cs:** `RouteCategories`, `RouteDifficulty` (Labels/Colors), `RouteRegions`.
