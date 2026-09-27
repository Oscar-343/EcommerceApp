using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Models.Inventario;

namespace EcommerceApp.Services
{
    // Abastecimientos: registrar el ingreso de mercadería (suma stock) y consultar el historial.
    // Un abastecimiento registrado no se edita ni se borra: así historial y stock siempre coinciden.
    public class AbastecimientoService(ApplicationDbContext context)
    {
        // Tope de productos por abastecimiento: evita formularios gigantes (manipulados) que
        // armen una transacción enorme. 100 filas alcanza de sobra para una entrega real.
        public const int MaxLineas = 100;

        // ---------------------------------------------------------------
        // REGISTRAR (lo más importante del módulo)
        // ---------------------------------------------------------------
        // "usuario" sale de User.Identity.Name en el controlador, nunca del formulario.
        public async Task<ResultadoOperacion> RegistrarAsync(AbastecimientoFormViewModel model, string usuario)
        {
            // 1. Validaciones de negocio (el servidor revisa todo; el JavaScript solo ayuda en pantalla).
            if (model.Lineas.Count == 0)
                return ResultadoOperacion.Falla("Agrega al menos un producto.");

            if (model.Lineas.Count > MaxLineas)
                return ResultadoOperacion.Falla($"Un abastecimiento puede tener como máximo {MaxLineas} productos.");

            if (model.Lineas.Any(l => l.Cantidad < 1 || l.Cantidad > 10000))
                return ResultadoOperacion.Falla("Hay una cantidad inválida (debe estar entre 1 y 10.000).");

            if (model.Lineas.Any(l => l.CostoUnitario <= 0 || l.CostoUnitario > 1000000))
                return ResultadoOperacion.Falla("Hay un costo unitario inválido.");

            // Producto repetido: se rechaza (más simple que sumar cantidades).
            if (model.Lineas.GroupBy(l => l.ProductoId).Any(g => g.Count() > 1))
                return ResultadoOperacion.Falla("Hay productos repetidos. Cada producto debe ir en una sola fila.");

            var proveedor = await context.Proveedores
                .Include(p => p.Marcas)
                .FirstOrDefaultAsync(p => p.Id == model.ProveedorId);

            if (proveedor == null)
                return ResultadoOperacion.Falla("El proveedor no existe.");
            if (!proveedor.Activo)
                return ResultadoOperacion.Falla($"El proveedor \"{proveedor.Nombre}\" está inactivo.");

            var idsProductos = model.Lineas.Select(l => l.ProductoId).ToList();
            var productos = await context.Products.AsNoTracking()
                .Where(p => idsProductos.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            if (productos.Count != idsProductos.Count)
                return ResultadoOperacion.Falla("Uno de los productos ya no existe. Recarga la página.");

            // Cada producto debe ser de una marca que el proveedor distribuye.
            var marcasDelProveedor = proveedor.Marcas.Select(pm => pm.MarcaId).ToHashSet();
            var ajeno = productos.Values.FirstOrDefault(p => p.MarcaId == null || !marcasDelProveedor.Contains(p.MarcaId.Value));
            if (ajeno != null)
                return ResultadoOperacion.Falla($"\"{ajeno.Name}\" no es de una marca que distribuya {proveedor.Nombre}.");

            // 2. Transacción: o se guarda TODO (cabecera + detalles + stock), o no se guarda NADA.
            await using var transaccion = await context.Database.BeginTransactionAsync();

            var abastecimiento = new Abastecimiento
            {
                ProveedorId = proveedor.Id,
                Fecha = DateTime.UtcNow,
                NumeroComprobante = Limpiar(model.NumeroComprobante),
                Observaciones = Limpiar(model.Observaciones),
                RegistradoPor = usuario
            };

            foreach (var linea in model.Lineas)
            {
                abastecimiento.Detalles.Add(new DetalleAbastecimiento
                {
                    ProductoId = linea.ProductoId,
                    NombreProducto = productos[linea.ProductoId].Name,   // se guarda por si el producto se borra
                    Cantidad = linea.Cantidad,
                    CostoUnitario = linea.CostoUnitario,
                    Subtotal = linea.Cantidad * linea.CostoUnitario      // calculado en el servidor
                });

                // Aquí se abastece la tienda. Suma sobre el valor actual en la BD en una sola
                // operación (igual que al cancelar un pedido), así no se pisa una venta simultánea.
                var filas = await context.Products
                    .Where(p => p.Id == linea.ProductoId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + linea.Cantidad));

                if (filas == 0)
                {
                    await transaccion.RollbackAsync();
                    return ResultadoOperacion.Falla($"\"{productos[linea.ProductoId].Name}\" ya no existe. No se registró nada.");
                }
            }

            abastecimiento.Total = abastecimiento.Detalles.Sum(d => d.Subtotal);

            context.Abastecimientos.Add(abastecimiento);
            await context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return ResultadoOperacion.Ok(abastecimiento.Id);
        }

        // ---------------------------------------------------------------
        // CONSULTAS
        // ---------------------------------------------------------------

        // Historial con filtros opcionales. Las fechas son días en hora Bolivia (inclusive).
        // Filtrar por marca devuelve los abastecimientos que incluyen algún producto de esa marca.
        public Task<List<Abastecimiento>> ListarAsync(int? proveedorId, int? marcaId, DateOnly? desde, DateOnly? hasta)
        {
            var query = context.Abastecimientos.AsNoTracking()
                .Include(a => a.Proveedor)
                .Include(a => a.Detalles)
                .AsQueryable();

            if (proveedorId.HasValue)
                query = query.Where(a => a.ProveedorId == proveedorId.Value);

            if (marcaId.HasValue)
                query = query.Where(a => a.Detalles.Any(d => d.Producto != null && d.Producto.MarcaId == marcaId.Value));

            if (desde.HasValue)
            {
                var desdeUtc = InicioDelDiaUtc(desde.Value);
                query = query.Where(a => a.Fecha >= desdeUtc);
            }

            if (hasta.HasValue)
            {
                var hastaUtcExclusivo = InicioDelDiaUtc(hasta.Value.AddDays(1));
                query = query.Where(a => a.Fecha < hastaUtcExclusivo);
            }

            return query
                .OrderByDescending(a => a.Fecha)
                .AsSplitQuery()
                .ToListAsync();
        }

        // Comprobante de un abastecimiento con sus productos (y la marca de cada uno, si sigue existiendo).
        public Task<Abastecimiento?> ObtenerDetalleAsync(int id) =>
            context.Abastecimientos.AsNoTracking()
                .Include(a => a.Proveedor)
                .Include(a => a.Detalles).ThenInclude(d => d.Producto).ThenInclude(p => p!.Marca)
                .AsSplitQuery()
                .FirstOrDefaultAsync(a => a.Id == id);

        // Productos que se le pueden comprar a un proveedor: los de las marcas que distribuye.
        public Task<List<Product>> ProductosPorProveedorAsync(int proveedorId) =>
            context.Products.AsNoTracking()
                .Include(p => p.Marca)
                .Where(p => p.MarcaId != null &&
                            context.ProveedorMarcas.Any(pm => pm.ProveedorId == proveedorId && pm.MarcaId == p.MarcaId))
                .OrderBy(p => p.Name)
                .ToListAsync();

        // Proveedores activos a los que se les puede comprar un producto (distribuyen su marca).
        // Lo usa el botón "Abastecer" de la lista de productos para preseleccionar el proveedor.
        public Task<List<int>> ProveedoresDeProductoAsync(int productoId) =>
            context.Products
                .Where(p => p.Id == productoId && p.MarcaId != null)
                .SelectMany(p => context.ProveedorMarcas
                    .Where(pm => pm.MarcaId == p.MarcaId && pm.Proveedor!.Activo)
                    .Select(pm => pm.ProveedorId))
                .ToListAsync();

        // Tarjeta del dashboard: cuántos abastecimientos hubo en el mes en curso (hora Bolivia) y cuánto sumaron.
        public async Task<(int Cantidad, decimal Total)> ResumenDelMesAsync()
        {
            var (primerDia, _) = ReportService.DefaultRange(null, null);
            var desdeUtc = InicioDelDiaUtc(primerDia);
            var hastaUtcExclusivo = InicioDelDiaUtc(primerDia.AddMonths(1));

            var delMes = context.Abastecimientos.Where(a => a.Fecha >= desdeUtc && a.Fecha < hastaUtcExclusivo);
            var cantidad = await delMes.CountAsync();
            var total = await delMes.SumAsync(a => (decimal?)a.Total) ?? 0m;
            return (cantidad, total);
        }

        // Lista breve "Últimos abastecimientos" del dashboard.
        public Task<List<Abastecimiento>> UltimosAsync(int cantidad) =>
            context.Abastecimientos.AsNoTracking()
                .Include(a => a.Proveedor)
                .Include(a => a.Detalles)
                .OrderByDescending(a => a.Fecha)
                .Take(cantidad)
                .AsSplitQuery()
                .ToListAsync();

        // Medianoche de un día en Bolivia, expresada en UTC (las fechas se guardan en UTC).
        private static DateTime InicioDelDiaUtc(DateOnly dia) =>
            DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue).AddHours(-ReportService.BoliviaOffsetHours), DateTimeKind.Utc);

        private static string? Limpiar(string? texto) =>
            string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }
}
