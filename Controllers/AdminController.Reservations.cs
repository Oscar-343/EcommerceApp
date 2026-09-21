using EcommerceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    // Sección Reservas del panel de administración.
    public partial class AdminController
    {
        // ---------------------------------------------------------------
        // RESERVAS
        // ---------------------------------------------------------------

        public async Task<IActionResult> Reservations(string? status)
        {
            ViewData["Title"] = "Reservas";
            ViewData["Subtitle"] = "Reservas de rutas hechas por los usuarios";

            var query = context.Reservations.AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Service)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            ViewBag.StatusFilter = status;
            var reservations = await query.OrderByDescending(r => r.TripDate).ToListAsync();
            return View(reservations);
        }

        // Transiciones válidas: Pendiente->Recorrido->Acabado; Pendiente o Recorrido->Cancelado.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservationUpdateStatus(int id, string status)
        {
            var reservation = await context.Reservations.FindAsync(id);
            if (reservation == null) return NotFound();

            var transicionValida =
                (reservation.Status == "Pendiente" && (status == "Recorrido" || status == "Cancelado")) ||
                (reservation.Status == "Recorrido" && (status == "Acabado" || status == "Cancelado"));

            if (!transicionValida)
            {
                TempData["Success"] = $"No se puede pasar de \"{reservation.Status}\" a \"{status}\".";
                return RedirectToAction(nameof(Reservations));
            }

            if (status == "Acabado")
            {
                reservation.CompletedAt = DateTime.UtcNow;
            }

            reservation.Status = status;
            await context.SaveChangesAsync();
            TempData["Success"] = "Estado de la reserva actualizado.";
            return RedirectToAction(nameof(Reservations));
        }

        // Muestra el formulario para crear una nueva reserva.
        [HttpGet]
        public async Task<IActionResult> ReservationCreate()
        {
            ViewData["Title"] = "Nueva reserva";
            ViewData["Subtitle"] = "Reservas";
            await LoadReservationLookupsAsync();
            // Se pasa modelo vacío para que el formulario no falle con null
            return View(new Reservation());
        }

        // Crea una nueva reserva con los datos del formulario.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservationCreate(Reservation reservation)
        {
            ViewData["Title"] = "Nueva reserva";
            ViewData["Subtitle"] = "Reservas";
            if (!ModelState.IsValid)
            {
                await LoadReservationLookupsAsync();
                return View(reservation);
            }

            // Verificar que no exista otra reserva para el mismo usuario, servicio y fecha de salida.
            var exists = await context.Reservations.AnyAsync(r =>
                r.UserId == reservation.UserId && r.ServiceId == reservation.ServiceId && r.TripDate == reservation.TripDate);
            if (exists)
            {
                ModelState.AddModelError(string.Empty, "Ya existe una reserva para este usuario, ruta y fecha de salida. Use edición para modificarla.");
                await LoadReservationLookupsAsync();
                return View(reservation);
            }

            // Obtener precio unitario del servicio y calcular total.
            var service = await context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == reservation.ServiceId);
            if (service != null)
            {
                reservation.UnitPrice = service.Price;
            }
            reservation.TotalPrice = reservation.UnitPrice * reservation.PeopleCount;
            reservation.BookingDate = DateTime.UtcNow;

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();
            TempData["Success"] = "Reserva creada correctamente.";
            return RedirectToAction(nameof(Reservations));
        }

        // Muestra el formulario para editar una reserva existente.
        [HttpGet]
        public async Task<IActionResult> ReservationEdit(int id)
        {
            ViewData["Title"] = "Editar reserva";
            ViewData["Subtitle"] = "Reservas";
            var reservation = await context.Reservations.FindAsync(id);
            if (reservation == null) return NotFound();
            await LoadReservationLookupsAsync();
            return View(reservation);
        }

        // Guarda los cambios de una reserva existente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservationEdit(int id, Reservation reservation)
        {
            ViewData["Title"] = "Editar reserva";
            ViewData["Subtitle"] = "Reservas";
            if (reservation.Id != id) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadReservationLookupsAsync();
                return View(reservation);
            }

            var existing = await context.Reservations.FindAsync(id);
            if (existing == null) return NotFound();

            // Convertir fecha a UTC para evitar error de PostgreSQL con DateTime Kind=Unspecified
            if (reservation.BookingDate.Kind == DateTimeKind.Unspecified)
                reservation.BookingDate = DateTime.SpecifyKind(reservation.BookingDate, DateTimeKind.Utc);

            existing.UserId = reservation.UserId;
            existing.ServiceId = reservation.ServiceId;
            existing.TripDate = reservation.TripDate;
            existing.PeopleCount = reservation.PeopleCount;
            existing.UnitPrice = reservation.UnitPrice;
            existing.TotalPrice = reservation.UnitPrice * reservation.PeopleCount;
            existing.Status = reservation.Status;
            existing.BookingDate = reservation.BookingDate;

            await context.SaveChangesAsync();
            TempData["Success"] = "Reserva actualizada correctamente.";
            return RedirectToAction(nameof(Reservations));
        }

        // Elimina permanentemente una reserva. Una reserva Acabado representa un ingreso ya
        // contado y no se puede borrar; Pendiente y Cancelado sí.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservationDelete(int id)
        {
            var reservation = await context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                TempData["Success"] = "Reserva no encontrada.";
                return RedirectToAction(nameof(Reservations));
            }

            if (reservation.Status == "Acabado")
            {
                TempData["Success"] = "No se puede eliminar una reserva Acabado.";
                return RedirectToAction(nameof(Reservations));
            }

            context.Reservations.Remove(reservation);
            await context.SaveChangesAsync();
            TempData["Success"] = "Reserva eliminada permanentemente.";
            return RedirectToAction(nameof(Reservations));
        }

        // Carga las listas de usuarios y servicios activos para los formularios de reservas.
        private async Task LoadReservationLookupsAsync()
        {
            ViewBag.Usuarios = await context.Users.AsNoTracking()
                .Select(u => new { u.Id, Email = u.Email ?? u.UserName ?? "", u.FullName })
                .OrderBy(u => u.Email)
                .ToListAsync();
            ViewBag.Servicios = await context.Services.AsNoTracking()
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
    }
}
