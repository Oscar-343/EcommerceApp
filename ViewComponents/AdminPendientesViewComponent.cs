using EcommerceApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.ViewComponents
{
    // Badge del menú lateral del panel admin con los pendientes de una sección ("reservas" o "pedidos").
    // Antes se calculaba en AdminController; como ViewComponent funciona en cualquier controlador
    // que use _AdminLayout (Proveedores, Marcas, Abastecimientos...).
    public class AdminPendientesViewComponent(ApplicationDbContext context) : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(string tipo)
        {
            int cantidad;
            try
            {
                cantidad = tipo switch
                {
                    "reservas" => await context.Reservations.CountAsync(r => r.Status == "Pendiente"),
                    "pedidos" => await context.Orders.CountAsync(o => o.Status == "Pendiente"),
                    _ => 0
                };
            }
            catch
            {
                // Si la BD falla, el menú se muestra igual, solo sin el número.
                cantidad = 0;
            }

            return View(cantidad);
        }
    }
}
