using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    // Reservas de rutas hechas por el propio usuario desde Services/Details.
    [Authorize]
    public class ReservationsController(ApplicationDbContext context) : Controller
    {
        // Crea una reserva nueva validando ruta, fecha, cupo y duplicados.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int serviceId, DateOnly tripDate, int peopleCount)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var service = await context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == serviceId);

            if (service == null || service.Status != "Active")
            {
                TempData["Success"] = "Esta ruta no está disponible para reservar.";
                return RedirectToAction("Index", "Services");
            }

            // "Desde mañana" en hora de Bolivia (UTC-4), no en UTC del servidor.
            var hoyBolivia = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-4));
            if (tripDate <= hoyBolivia)
            {
                TempData["Success"] = "La fecha de salida debe ser desde mañana.";
                return RedirectToAction("Details", "Services", new { id = serviceId });
            }

            if (peopleCount < 1 || peopleCount > service.MaxGroupSize)
            {
                TempData["Success"] = $"La cantidad de personas debe ser entre 1 y {service.MaxGroupSize}.";
                return RedirectToAction("Details", "Services", new { id = serviceId });
            }

            await using var transaction = await context.Database.BeginTransactionAsync();

            var yaReservo = await context.Reservations.AnyAsync(r =>
                r.UserId == userId && r.ServiceId == serviceId && r.TripDate == tripDate);
            if (yaReservo)
            {
                TempData["Success"] = "Ya tienes una reserva para esta ruta en esa fecha.";
                return RedirectToAction("Details", "Services", new { id = serviceId });
            }

            // Capacidad de esa fecha: personas ya reservadas (no canceladas) para esa ruta y fecha.
            var personasReservadas = await context.Reservations
                .Where(r => r.ServiceId == serviceId && r.TripDate == tripDate && r.Status != "Cancelado")
                .SumAsync(r => (int?)r.PeopleCount) ?? 0;

            if (personasReservadas + peopleCount > service.MaxGroupSize)
            {
                TempData["Success"] = $"No hay cupo suficiente para esa fecha (quedan {service.MaxGroupSize - personasReservadas} lugar(es)).";
                return RedirectToAction("Details", "Services", new { id = serviceId });
            }

            var reservation = new Reservation
            {
                UserId = userId,
                ServiceId = serviceId,
                TripDate = tripDate,
                PeopleCount = peopleCount,
                UnitPrice = service.Price,
                TotalPrice = service.Price * peopleCount,
                Status = "Pendiente",
                BookingDate = DateTime.UtcNow
            };

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = "Reserva creada. Queda pendiente de confirmación.";
            return RedirectToAction(nameof(Index));
        }

        // "Mis reservas": lista las del usuario actual, más recientes primero.
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var reservations = await context.Reservations
                .AsNoTracking()
                .Include(r => r.Service)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.BookingDate)
                .ToListAsync();

            ViewData["Title"] = "Mis reservas";
            return View(reservations);
        }

        // Cancela una reserva propia mientras siga Pendiente.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var reservation = await context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reservation == null) return NotFound();

            if (reservation.Status != "Pendiente")
            {
                TempData["Success"] = "Solo se pueden cancelar reservas pendientes.";
                return RedirectToAction(nameof(Index));
            }

            reservation.Status = "Cancelado";
            await context.SaveChangesAsync();

            TempData["Success"] = "Reserva cancelada.";
            return RedirectToAction(nameof(Index));
        }
    }
}
