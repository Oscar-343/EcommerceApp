using EcommerceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    // Sección Transporte del panel de administración.
    public partial class AdminController
    {
        // ---------------------------------------------------------------
        // TRANSPORTE
        // ---------------------------------------------------------------

        public async Task<IActionResult> Transports(string? q)
        {
            ViewData["Title"] = "Transporte";
            ViewData["Subtitle"] = "Vehículos disponibles para traslados";

            var query = context.Transports.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(t => t.Name.ToLower().Contains(q.ToLower()) || (t.Type != null && t.Type.ToLower().Contains(q.ToLower())));
            }

            ViewBag.Query = q;
            var transports = await query.OrderBy(t => t.Name).ToListAsync();

            var counts = await context.Services
                .Where(s => s.TransportId != null)
                .GroupBy(s => s.TransportId)
                .Select(g => new { TransportId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TransportId!.Value, x => x.Count);
            ViewBag.RouteCounts = counts;

            return View(transports);
        }

        public IActionResult TransportCreate()
        {
            ViewData["Title"] = "Nuevo transporte";
            ViewData["Subtitle"] = "Transporte";
            return View(new Transport());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransportCreate(Transport transport)
        {
            ViewData["Title"] = "Nuevo transporte";
            ViewData["Subtitle"] = "Transporte";
            if (!ModelState.IsValid) return View(transport);

            context.Transports.Add(transport);
            await context.SaveChangesAsync();
            TempData["Success"] = $"Transporte \"{transport.Name}\" agregado.";
            return RedirectToAction(nameof(Transports));
        }

        public async Task<IActionResult> TransportEdit(int id)
        {
            ViewData["Title"] = "Editar transporte";
            ViewData["Subtitle"] = "Transporte";
            var transport = await context.Transports.FindAsync(id);
            if (transport == null) return NotFound();
            return View(transport);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransportEdit(int id, Transport transport)
        {
            ViewData["Title"] = "Editar transporte";
            ViewData["Subtitle"] = "Transporte";
            if (id != transport.Id) return NotFound();
            if (!ModelState.IsValid) return View(transport);

            transport.UpdatedAt = DateTime.UtcNow;
            context.Transports.Update(transport);
            // Update() marca todo como modificado; CreatedAt no debe pisarse con el valor del formulario.
            context.Entry(transport).Property(t => t.CreatedAt).IsModified = false;
            await context.SaveChangesAsync();
            TempData["Success"] = $"Transporte \"{transport.Name}\" actualizado.";
            return RedirectToAction(nameof(Transports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransportDelete(int id)
        {
            var transport = await context.Transports.FindAsync(id);
            if (transport != null)
            {
                var inUse = await context.Services.AnyAsync(s => s.TransportId == id);
                if (inUse)
                {
                    transport.IsActive = false;
                    transport.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    TempData["Success"] = $"\"{transport.Name}\" está asignado a rutas, así que se marcó como inactivo en vez de eliminarse.";
                }
                else
                {
                    context.Transports.Remove(transport);
                    await context.SaveChangesAsync();
                    TempData["Success"] = $"Transporte \"{transport.Name}\" eliminado.";
                }
            }
            return RedirectToAction(nameof(Transports));
        }
    }
}
