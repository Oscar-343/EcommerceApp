using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Models.Inventario;

namespace EcommerceApp.Services
{
    // Gestión de marcas (la usan MarcasController, ProveedoresController y el formulario de productos).
    // Las marcas no se borran: se desactivan.
    public class MarcaService(ApplicationDbContext context)
    {
        // Lista simple, ordenada por nombre (para selects y chips).
        public Task<List<Marca>> ListarAsync(bool soloActivas = false)
        {
            var query = context.Marcas.AsNoTracking().AsQueryable();
            if (soloActivas)
                query = query.Where(m => m.Activo);

            return query.OrderBy(m => m.Nombre).ToListAsync();
        }

        // Lista con cuántos productos y proveedores tiene cada marca (grid de Marcas/Index).
        public Task<List<MarcaListaItem>> ListarResumenAsync() =>
            context.Marcas.AsNoTracking()
                .OrderBy(m => m.Nombre)
                .Select(m => new MarcaListaItem
                {
                    Id = m.Id,
                    Nombre = m.Nombre,
                    LogoUrl = m.LogoUrl,
                    Activo = m.Activo,
                    CantidadProductos = m.Productos.Count,
                    CantidadProveedores = m.Proveedores.Count
                })
                .ToListAsync();

        public Task<Marca?> ObtenerAsync(int id) =>
            context.Marcas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

        public async Task<ResultadoOperacion> CrearAsync(string nombre, string? logoUrl)
        {
            nombre = nombre.Trim();
            if (await NombreEnUsoAsync(nombre, idExcluido: null))
                return ResultadoOperacion.Falla($"Ya existe una marca llamada \"{nombre}\".");

            var marca = new Marca { Nombre = nombre, LogoUrl = Limpiar(logoUrl) };
            context.Marcas.Add(marca);
            return await GuardarAsync(marca, nombre);
        }

        public async Task<ResultadoOperacion> ActualizarAsync(int id, string nombre, string? logoUrl)
        {
            var marca = await context.Marcas.FindAsync(id);
            if (marca == null)
                return ResultadoOperacion.Falla("La marca no existe.");

            nombre = nombre.Trim();
            if (await NombreEnUsoAsync(nombre, idExcluido: id))
                return ResultadoOperacion.Falla($"Ya existe otra marca llamada \"{nombre}\".");

            marca.Nombre = nombre;
            marca.LogoUrl = Limpiar(logoUrl);
            return await GuardarAsync(marca, nombre);
        }

        // Una marca no se puede desactivar mientras tenga productos en el catálogo
        // (los productos no tienen estado activo/inactivo: si existen, están a la venta).
        public async Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activo)
        {
            var marca = await context.Marcas.FindAsync(id);
            if (marca == null)
                return ResultadoOperacion.Falla("La marca no existe.");

            if (!activo)
            {
                var productos = await context.Products.CountAsync(p => p.MarcaId == id);
                if (productos > 0)
                    return ResultadoOperacion.Falla(
                        $"No se puede desactivar \"{marca.Nombre}\": tiene {productos} producto(s) en el catálogo. Cámbialos de marca primero.");
            }

            marca.Activo = activo;
            await context.SaveChangesAsync();
            return ResultadoOperacion.Ok(marca.Id);
        }

        // Si otro admin guardó el mismo nombre justo antes, el índice único de la BD lo rechaza:
        // se devuelve un mensaje en vez de una página de error.
        private async Task<ResultadoOperacion> GuardarAsync(Marca marca, string nombre)
        {
            try
            {
                await context.SaveChangesAsync();
                return ResultadoOperacion.Ok(marca.Id);
            }
            catch (DbUpdateException)
            {
                return ResultadoOperacion.Falla($"Ya existe una marca llamada \"{nombre}\".");
            }
        }

        // Compara sin distinguir mayúsculas: "Columbia" y "columbia" son la misma marca.
        private Task<bool> NombreEnUsoAsync(string nombre, int? idExcluido)
        {
            var nombreMinusculas = nombre.ToLower();
            return context.Marcas.AnyAsync(m => m.Nombre.ToLower() == nombreMinusculas && m.Id != idExcluido);
        }

        private static string? Limpiar(string? texto) =>
            string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }
}
