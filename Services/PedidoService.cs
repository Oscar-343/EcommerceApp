using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Services
{
    // Datos extra que el checkout guarda en el pedido (entrega + pago simulado).
    // Ya vienen validados por el controlador; aquí no hay datos completos de tarjeta.
    public record DatosCheckout(
        string NombreCompleto, string Telefono, string Email, string Ciudad, string Direccion, string? Referencia,
        string MetodoPago, string? TarjetaMarca, string? TarjetaUltimos4, string CodigoTransaccion);

    // Resultado de crear un pedido: el pedido creado, o el mensaje de por qué no se pudo.
    public record ResultadoPedido(Order? Pedido, string? Error);

    // Crea pedidos a partir del carrito del usuario (lo usan OrdersController y CheckoutController).
    public class PedidoService(ApplicationDbContext context)
    {
        // Carrito del usuario con sus productos (precios y stock actuales de la BD).
        public Task<List<CartItem>> ObtenerCarritoAsync(string userId) =>
            context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

        // Revisa que el carrito se pueda comprar. Devuelve null si está todo bien, o el mensaje para el usuario.
        public static string? ValidarCarrito(List<CartItem> items)
        {
            if (items.Count == 0) return "Tu carrito está vacío.";

            // Revalidar stock en servidor: nunca confiar en lo que se veía en pantalla.
            foreach (var item in items)
            {
                if (item.Product == null)
                    return "Un producto de tu carrito ya no existe. Quítalo para continuar.";

                if (item.Product.Stock <= 0 || item.Quantity > item.Product.Stock)
                    return $"\"{item.Product.Name}\" no tiene stock suficiente. Ajusta la cantidad para continuar.";
            }
            return null;
        }

        // Resumen "Tu equipo" con el precio vigente de cada producto. Sin envío ni impuestos: Total = Subtotal.
        public static ResumenCompraViewModel ArmarResumen(List<CartItem> items)
        {
            var resumen = new ResumenCompraViewModel
            {
                Items = items
                    .Where(i => i.Product != null)
                    .Select(i => new ItemResumen
                    {
                        Nombre = i.Product!.Name,
                        ImagenUrl = i.Product.ImageUrl,
                        Cantidad = i.Quantity,
                        PrecioUnitario = i.Product.PromotionalPrice ?? i.Product.Price
                    })
                    .ToList()
            };
            resumen.Subtotal = resumen.Items.Sum(i => i.Subtotal);
            resumen.Total = resumen.Subtotal;
            return resumen;
        }

        // Convierte el carrito en un pedido "Pendiente": descuenta stock, guarda las líneas y vacía el carrito.
        // "datos" solo llega desde el checkout; al confirmar directo desde el carrito es null.
        public async Task<ResultadoPedido> CrearDesdeCarritoAsync(string userId, DatosCheckout? datos = null)
        {
            var items = await ObtenerCarritoAsync(userId);

            var error = ValidarCarrito(items);
            if (error != null) return new ResultadoPedido(null, error);

            await using var transaction = await context.Database.BeginTransactionAsync();

            foreach (var item in items)
            {
                // Descuenta solo si todavía hay stock suficiente. Es una sola operación en la BD,
                // así dos compras simultáneas no pueden vender la misma unidad.
                var filas = await context.Products
                    .Where(p => p.Id == item.ProductId && p.Stock >= item.Quantity)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity));

                if (filas == 0)
                {
                    await transaction.RollbackAsync();
                    return new ResultadoPedido(null, $"\"{item.Product!.Name}\" ya no tiene stock suficiente. Ajusta la cantidad para continuar.");
                }
            }

            // El pedido nace Pendiente aunque el pago simulado se apruebe:
            // solo cuenta como ingreso cuando el admin lo marca Entregado.
            var order = new Order
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = "Pendiente"
            };

            if (datos != null)
            {
                order.NombreEntrega = datos.NombreCompleto;
                order.TelefonoEntrega = datos.Telefono;
                order.EmailEntrega = datos.Email;
                order.CiudadEntrega = datos.Ciudad;
                order.DireccionEntrega = datos.Direccion;
                order.ReferenciaEntrega = datos.Referencia;
                order.MetodoPago = datos.MetodoPago;
                order.TarjetaMarca = datos.TarjetaMarca;
                order.TarjetaUltimos4 = datos.TarjetaUltimos4;
                order.CodigoTransaccion = datos.CodigoTransaccion;
            }

            foreach (var item in items)
            {
                var product = item.Product!;
                var precioVigente = product.PromotionalPrice ?? product.Price;

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductCategory = product.Category,
                    UnitPrice = precioVigente,
                    Quantity = item.Quantity
                });
            }

            order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            context.Orders.Add(order);
            context.CartItems.RemoveRange(items);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new ResultadoPedido(order, null);
        }
    }
}
