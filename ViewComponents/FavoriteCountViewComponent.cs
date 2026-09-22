using EcommerceApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.ViewComponents
{
    // Contador de favoritos, mostrado como badge junto al ícono de favoritos en la navbar.
    // Espejo de CartCountViewComponent.
    public class FavoriteCountViewComponent(ApplicationDbContext context) : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity?.IsAuthenticated != true) return Content("");

            var userId = (User as System.Security.Claims.ClaimsPrincipal)?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var count = await context.FavoriteItems.Where(f => f.UserId == userId).CountAsync();

            return View(count);
        }
    }
}
