# Tareas pendientes

## Reservas (`Models/Reservation.cs`)

- La clave primaria es compuesta `(UserId, ServiceId)`: un usuario solo puede tener **una** reserva activa por ruta a la vez (si reserva de nuevo la misma ruta, pisa la reserva anterior en vez de crear una nueva).
- No hay campo de **fecha de salida** de la ruta: `BookingDate` es la fecha en que se hizo la reserva, no la fecha en que se realiza el recorrido.
- No se tocó en la Fase 5 (el plan lo deja explícitamente fuera). Si se quiere implementar reservas reales con múltiples fechas por usuario, hace falta:
  - Cambiar la PK a un `Id` propio (autoincremental) y dejar `(UserId, ServiceId)` como índice no único.
  - Agregar un campo `TripDate` (fecha del recorrido, distinta de `BookingDate`).
  - Revisar `AdminController.ReservationCreate/Edit` y las vistas de reservas, que hoy asumen una reserva por usuario/ruta.
