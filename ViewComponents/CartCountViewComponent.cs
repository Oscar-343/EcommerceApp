using EcommerceApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.ViewComponents
{
    // Contador de items del carrito, mostrado como badge junto al link "Carrito" en la navbar.
    public class CartCountViewComponent(ApplicationDbContext context) : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity?.IsAuthenticated != true) return Content("");

            var userId = (User as System.Security.Claims.ClaimsPrincipal)?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var count = await context.CartItems.Where(c => c.UserId == userId).SumAsync(c => (int?)c.Quantity) ?? 0;

            return View(count);
        }
    }
}
