using EcommerceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    // Sección Guías del panel de administración.
    public partial class AdminController
    {
        // ---------------------------------------------------------------
        // GUÍAS
        // ---------------------------------------------------------------

        public async Task<IActionResult> Guides(string? q)
        {
            ViewData["Title"] = "Guías";
            ViewData["Subtitle"] = "Equipo de guías de montaña";

            var query = context.Guides.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(g => g.Name.ToLower().Contains(q.ToLower()) || (g.Specialty != null && g.Specialty.ToLower().Contains(q.ToLower())));
            }

            ViewBag.Query = q;
            var guides = await query.OrderBy(g => g.Name).ToListAsync();

            // Cantidad de rutas asignadas por guía, para mostrar en la tabla.
            var counts = await context.Services
                .Where(s => s.GuideId != null)
                .GroupBy(s => s.GuideId)
                .Select(g => new { GuideId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GuideId!.Value, x => x.Count);
            ViewBag.RouteCounts = counts;

            return View(guides);
        }

        public IActionResult GuideCreate()
        {
            ViewData["Title"] = "Nuevo guía";
            ViewData["Subtitle"] = "Guías";
            return View(new Guide());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuideCreate(Guide guide)
        {
            ViewData["Title"] = "Nuevo guía";
            ViewData["Subtitle"] = "Guías";
            if (!ModelState.IsValid) return View(guide);

            context.Guides.Add(guide);
            await context.SaveChangesAsync();
            TempData["Success"] = $"Guía \"{guide.Name}\" agregado.";
            return RedirectToAction(nameof(Guides));
        }

        public async Task<IActionResult> GuideEdit(int id)
        {
            ViewData["Title"] = "Editar guía";
            ViewData["Subtitle"] = "Guías";
            var guide = await context.Guides.FindAsync(id);
            if (guide == null) return NotFound();
            return View(guide);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuideEdit(int id, Guide guide)
        {
            ViewData["Title"] = "Editar guía";
            ViewData["Subtitle"] = "Guías";
            if (id != guide.Id) return NotFound();
            if (!ModelState.IsValid) return View(guide);

            guide.UpdatedAt = DateTime.UtcNow;
            context.Guides.Update(guide);
            // Update() marca todo como modificado; CreatedAt no debe pisarse con el valor del formulario.
            context.Entry(guide).Property(g => g.CreatedAt).IsModified = false;
            await context.SaveChangesAsync();
            TempData["Success"] = $"Guía \"{guide.Name}\" actualizado.";
            return RedirectToAction(nameof(Guides));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuideDelete(int id)
        {
            var guide = await context.Guides.FindAsync(id);
            if (guide != null)
            {
                var inUse = await context.Services.AnyAsync(s => s.GuideId == id);
                if (inUse)
                {
                    // No se puede borrar: hay rutas que lo referencian. Se desactiva en su lugar.
                    guide.IsActive = false;
                    guide.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    TempData["Success"] = $"\"{guide.Name}\" tiene rutas asignadas, así que se marcó como inactivo en vez de eliminarse.";
                }
                else
                {
                    context.Guides.Remove(guide);
                    await context.SaveChangesAsync();
                    TempData["Success"] = $"Guía \"{guide.Name}\" eliminado.";
                }
            }
            return RedirectToAction(nameof(Guides));
        }
    }
}
