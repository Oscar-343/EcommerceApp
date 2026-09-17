# Project Memory — Tren al Sur

## Información general

- **Propósito (confirmado):** página web funcional para una tienda de productos y servicios de senderismo, vinculada al mercado argentino. Nombre del proyecto: **Tren al Sur**.
- **Origen:** el proyecto parte de código existente (actualmente con nombre interno "EcommerceApp"). Decisión confirmada: NO reconstruir desde cero, sino evolucionar el código existente.
- **Estado:** proyecto en etapa avanzada de desarrollo. Funcionalidades implementadas: tienda pública con filtros, panel de administración completo, login/registro social, recuperación de contraseña, subida de imágenes a Supabase Storage, seeder de datos, servicios inyectables (email, almacenamiento). Ver `.agent/TASKS.md` y `PROJECT_STATE.md` para tareas restantes.

## Stack confirmado

- Lenguaje: C#.
- Framework: ASP.NET Core.
- Arquitectura: MVC (Model-View-Controller).
- ORM: Entity Framework Core.
- Base de datos: PostgreSQL mediante Supabase (ya implementado en el código, ver DECISIONS.md).
- Autenticación: ASP.NET Core Identity.

## Arquitectura y estructura

- Solución: `EcommerceApp.slnx` (formato slnx) con **un único proyecto web** `EcommerceApp/EcommerceApp.csproj`.
- Estructura MVC clásica: `Controllers/`, `Models/` (entidades + ViewModels), `Views/` (Razor), `Data/` (DbContext), `Migrations/`.
- NO existen capas adicionales (Services, Repositories, DTOs, Areas) por el momento.
- Startup en `Program.cs` (top-level statements): DI de DbContext, Identity, cookies, MVC, ruta por defecto `{controller=Account}/{action=Login}/{id?}`.
- Se crean roles `Admin` y `User` en el arranque si no existen (bloque en `Program.cs`).

## Entidades y base de datos

- `Product`: Id, Name, Description, Price (numeric(18,2)), **PromotionalPrice (nullable, precio de oferta)**, **IsFeatured (bool, destacado en portada)**, Stock, ImageUrl, Category, CreatedAt, UpdatedAt.
- `ApplicationUser : IdentityUser`: FullName, Address, CreatedAt.
- **`Service` (servicio/ruta)** — tabla `Services`: Id, Category (senderismo, trekking, viajes de varios días...), Name, Description, Location, Difficulty, DistanceKm (double), Duration (string), Price (numeric(18,2)), Status, ImageUrl, CreatedAt, UpdatedAt.
- **`Reservation`** — tabla `Reservations`, **clave compuesta `(UserId, ServiceId)`** (FKs a AspNetUsers y Services, `OnDelete Restrict`): BookingDate, PeopleCount, UnitPrice, TotalPrice, Status (`Pendiente`, `Recorrido`, `Acabado`, `Cancelado`).
- **`Guide`** — tabla `Guides`: Id, Name, Specialty, Bio, Phone, Email, IsActive, CreatedAt, UpdatedAt.
- **`Transport`** — tabla `Transports`: Id, Name, Type, Capacity, Description, PricePerKm (numeric(18,2)), IsActive, CreatedAt, UpdatedAt.
- Tablas de Identity estándar + `Products` + `Services`, `Reservations`, `Guides`, `Transports`.
- Migraciones: `20260909154753_InitialCreate`, `20260916153408_AddBusinessEntities` (tablas de negocio), `20260916192453_AddProductPromotion` (**IsFeatured + PromotionalPrice en Products**, aplicada en Supabase el 2026-09-16). Aditivas y reversibles.
- **Convención de esquema:** columnas en PascalCase entre comillas, igual que el resto de la BD. El "id_ruta" del usuario = FK `ServiceId` en Reservations.
- **Limitación conocida de diseño:** la clave compuesta `(UserId, ServiceId)` permite UNA reserva por usuario y por ruta. Si se necesitan varias reservas de la misma ruta (fechas distintas), habría que cambiar el PK (p. ej. Id identity + índice único UserId/ServiceId/BookingDate).

## Convenciones detectadas en el código

- Constructor primario de C# 12+ (classes de controllers y DbContext).
- `Primary constructor` comentado: los controllers no declaran campos/constructores explícitos.
- Comentarios en español en el código.
- Validación con DataAnnotations + jQuery unobtrusive en cliente.
- Anti-forgery token en todos los POST.
- **Los botones de login social (Google/GitHub) van en formularios `form` propios y HERMANOS del formulario principal, nunca anidados dentro de él.** Un `<form>` dentro de otro `<form>` es HTML inválido: el navegador fusiona el contenido en el formulario exterior y el clic deja de llegar a `ExternalLogin` (regresión real detectada el 2026-09-15 en `Login.cshtml`).

## Convenciones del proyecto (confirmadas por el usuario)

- **Código sencillo, simple de entender y sin abstracciones innecesarias.**
- **Comentarios breves sobre funciones** (en español, estilo "qué hace y por qué").
- **Estilo similar a los códigos de ejemplo proporcionados por el docente**:
  - Uso de constructor primario.
  - Comentarios cortos y claros.
  - Sin sobrediseño ni patrones complejos.
- Esta convención aplica a todo el código nuevo; antes de agregar complejidad, preguntar.

## Funcionalidades implementadas

### Autenticación y acceso
- Registro/login local con ASP.NET Core Identity (roles Admin/User).
- Login social: Google y GitHub (credenciales en `appsettings.json`, proveedores registrados condicionalmente). Registro externo con `ExternalRegister` cuando GitHub no expone el email.
- Redirección por rol tras login (`RedirectForRole`).
- Recuperación de contraseña con token seguro (flujo completo, envío por SMTP configurable o consola).

### Panel de administración (`AdminController`)
- Dashboard con contadores de productos, servicios, guías, transportes, reservas y bajo stock. Badge de reservas pendientes en sidebar.
- CRUD completo de Productos (imagen principal + secundaria, upload a Supabase Storage).
- CRUD completo de Rutas/Servicios (imagen + secundaria + galería, upload a Supabase Storage).
- CRUD de Guías y Transportes (eliminación suave si hay rutas asignadas).
- Gestión de Reservas (filtro por estado, actualización de estado).
- Sección de Branding (subida de logo/video/imágenes de marca a Supabase Storage).

### Tienda pública
- `ProductsController.Index`: catálogo con filtros (categoría, marca, rango de precios, búsqueda), ordenamiento (featured, precio asc/desc, más recientes). AJAX dinámico (`GetFilteredProducts`, `GetFilterOptions`).
- `ProductsController.Details`: vista detallada de producto.
- `ServicesController.Index`: catálogo de rutas con filtros (región, dificultad, categoría, duración máxima, búsqueda), estadísticas. AJAX (`GetFilterOptions`).
- `ServicesController.Details`: vista detallada de ruta con galería de imágenes, includes/excludes/recommendations parseados, dificultad con colores/emojis.

### Modelo de datos ampliado
- `Product`: campos adicionales Brand, IsNew, IsBestSeller, SecondaryImageUrl.
- `Service`: campos extendidos ShortDescription, Region, StartPoint, EndPoint, DifficultyDescription, DurationHours, MaxGroupSize, PriceDescription, GalleryImages, Includes, Excludes, Recommendations, coordenadas GPS, GuideId/TransportId.
- `Reservation`: clave compuesta (UserId, ServiceId).
- `Guide` y `Transport`: catálogo con IsActive.

### Utilidades
- `SupabaseImageStorageService`: subida de archivos al Storage de Supabase con validación de tipo/tamaño.
- `SmtpEmailSender`: envío de correos por SMTP con fallback a consola.
- `ProductSeeder` y `ServiceSeeder`: semilla automática de datos de ejemplo.

## Restricciones y notas

- **Zonas INTOCABLES (definidas por el docente, proyecto universitario por fases):**
  - Arquitectura del proyecto.
  - Supabase y la base de datos en general (esquema, proveedor, conexión).
  - La sección `ConnectionStrings` (y el string de conexión) en los `.json` de configuración.
  - Cualquier código/configuración de esas áreas no se modifica bajo ninguna circunstancia; en caso de duda, preguntar.
- **Zona de trabajo actual:** autenticación/login (UI + OAuth). Se implementa sobre los mecanismos existentes de ASP.NET Core Identity sin tocar BD ni arquitectura.
- Git CLI **no está disponible** en el PATH de esta máquina (existe repo `.git` con remote GitHub `Oscar-343/EcommerceApp`, rama `master`, 2 commits).
- La cadena de conexión de la BD vive en `appsettings.json` en texto plano (incluye contraseña) → problema de seguridad conocido, ver PROJECT_STATE.md — PERO NO debe modificarse por ser zona del docente. No registrar secretos en memoria.
- La ruta por defecto obliga a login para entrar a la app; solo `Products/Index` y `Products/Details` son públicas (`[AllowAnonymous]`).

## Información pendiente de definir (NO inventar)

- Funcionalidades, entidades, tablas, relaciones, procesos de negocio, endpoints y reglas de negocio reales de la tienda → **pendiente de definir por el usuario**.
- Identidad visual/nombre mostrado (hoy todo dice "EcommerceApp").
- Modelo de datos definitivo (carrito, órdenes, categorías, etc.): pendiente.