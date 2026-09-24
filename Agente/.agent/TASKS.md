# Tareas pendientes

- **Fase 1 de `Agente/PLAN_HOME_MAPA_Y_CORRECCIONES.md` sin hacer:** video del hero del Home fijo al hacer scroll (`position: fixed` + `clip-path: inset(0)`).
- **Título del mapa del Home:** sigue "RUTAS CERCA DE TI"; el dueño definirá el texto nuevo (el mapa no usa la ubicación del usuario).
- **Coordenadas:** ninguna ruta tiene coordenadas cargadas todavía; hasta cargarlas, el mapa no aparece en el Home ni en Rutas.
- **`wwwroot/css/home.css` tiene dos `@@media`** (sintaxis de Razor dentro de un .css): el navegador ignora esos bloques responsive (768 px y 480 px).
- **`Guide.Name` y `Transport.Name` conservan `RegularExpression(@"^[\w\s\-]+$")`:** nombres con acentos no pasan la validación del navegador.
- **Gráficos de reportes (Chart.js) omitidos por decisión del usuario** (Fase 6 de `Agente/PLAN_REPORTES.md` los dejaba como opcional, "pregúntame antes"). Se puede pedir después si hace falta; no requiere cambios de modelo, solo vistas.
- **Enlaces de Google Maps de negocios o lugares con nombre (limitación conocida, no es un bug):** Google a veces redirige a una búsqueda por nombre sin coordenadas; el formulario muestra un aviso y el administrador debe marcar el punto exacto (pin) y volver a compartirlo, o pegar las coordenadas con clic derecho. Si Google cambia el formato de sus enlaces, los campos de latitud y longitud siguen funcionando como respaldo.

Reservas y pedidos ya no tienen pendientes conocidos: la reserva real (PK propia, `TripDate`, capacidad por fecha) y el flujo de pedidos (`Order`/`OrderItem`, checkout, cambio de estado) se implementaron completos en `Agente/PLAN_REPORTES.md` (Fases 2 y 3).
