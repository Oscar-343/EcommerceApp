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
            var reservations = await query.OrderByDescending(r => r.BookingDate).ToListAsync();
            return View(reservations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservationUpdateStatus(string userId, int serviceId, string status)
        {
            var estadosValidos = new[] { "Pendiente", "Recorrido", "Acabado", "Cancelado" };
            if (!estadosValidos.Contains(status))
            {
                TempData["Success"] = "Estado no válido.";
                return RedirectToAction(nameof(Reservations));
            }

            var reservation = await context.Reservations.FindAsync(userId, serviceId);
            if (reservation != null)
            {
                reservation.Status = status;
                await context.SaveChangesAsync();
                TempData["Success"] = "Estado de la reserva actualizado.";
            }
            return RedirectToAction(nameof(Reservations));
        }

        // Muestra el formulario para crear una nueva reserva.
        [HttpGet]
        public async Task<IActionResult> ReservationCreate()
        {
            ViewData["Title"] = "Nueva reserva";
            ViewData["Subtitle"] = "Reservas";
            ViewBag.Usuarios = await context.Users.AsNoTracking()
                .Select(u => new { u.Id, Email = u.Email ?? u.UserName ?? "", u.FullName })
                .OrderBy(u => u.Email)
                .ToListAsync();
            ViewBag.Servicios = await context.Services.AsNoTracking()
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.Name)
                .ToListAsync();
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

            // Verificar que no exista otra reserva para el mismo usuario-servicio (clave compuesta).
            var exists = await context.Reservations.AnyAsync(r => r.UserId == reservation.UserId && r.ServiceId == reservation.ServiceId);
            if (exists)
            {
                ModelState.AddModelError(string.Empty, "Ya existe una reserva para este usuario con este servicio. Use edición para modificarla.");
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
            TempData["Success"] = $"Reserva creada correctamente para {reservation.UserId} en la ruta {reservation.ServiceId}.";
            return RedirectToAction(nameof(Reservations));
        }

        // Muestra el formulario para editar una reserva existente.
        [HttpGet]
        public async Task<IActionResult> ReservationEdit(string userId, int serviceId)
        {
            ViewData["Title"] = "Editar reserva";
            ViewData["Subtitle"] = "Reservas";
            var reservation = await context.Reservations.FindAsync(userId, serviceId);
            if (reservation == null) return NotFound();
            await LoadReservationLookupsAsync();
            return View(reservation);
        }

        // Guarda los cambios de una reserva existente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservationEdit(string userId, int serviceId, Reservation reservation)
        {
            ViewData["Title"] = "Editar reserva";
            ViewData["Subtitle"] = "Reservas";
            if (reservation.UserId != userId || reservation.ServiceId != serviceId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadReservationLookupsAsync();
                return View(reservation);
            }

            var existing = await context.Reservations.FindAsync(userId, serviceId);
            if (existing == null) return NotFound();

            // Convertir fecha a UTC para evitar error de PostgreSQL con DateTime Kind=Unspecified
            if (reservation.BookingDate.Kind == DateTimeKind.Unspecified)
                reservation.BookingDate = DateTime.SpecifyKind(reservation.BookingDate, DateTimeKind.Utc);

            existing.PeopleCount = reservation.PeopleCount;
            existing.UnitPrice = reservation.UnitPrice;
            existing.TotalPrice = reservation.UnitPrice * reservation.PeopleCount;
            existing.Status = reservation.Status;
            existing.BookingDate = reservation.BookingDate;

            context.Reservations.Update(existing);
            await context.SaveChangesAsync();
            TempData["Success"] = "Reserva actualizada correctamente.";
            return RedirectToAction(nameof(Reservations));
        }

        // Elimina permanentemente una reserva.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservationDelete(string userId, int serviceId)
        {
            var reservation = await context.Reservations.FindAsync(userId, serviceId);
            if (reservation != null)
            {
                context.Reservations.Remove(reservation);
                await context.SaveChangesAsync();
                TempData["Success"] = "Reserva eliminada permanentemente.";
            }
            else
            {
                TempData["Success"] = "Reserva no encontrada.";
            }
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
