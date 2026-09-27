using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Models.Inventario;

namespace EcommerceApp.Services
{
    // Gestión de proveedores y de las marcas que distribuye cada uno.
    // Los proveedores no se borran: se desactivan, para conservar su historial.
    public class ProveedorService(ApplicationDbContext context)
    {
        // Listado con filtros opcionales: texto (nombre o NIT), marca que distribuye y estado.
        public Task<List<ProveedorListaItem>> ListarAsync(string? busqueda, int? marcaId, bool? activo)
        {
            var query = context.Proveedores.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var texto = busqueda.Trim().ToLower();
                query = query.Where(p => p.Nombre.ToLower().Contains(texto) ||
                                         (p.Nit != null && p.Nit.ToLower().Contains(texto)));
            }

            if (marcaId.HasValue)
                query = query.Where(p => p.Marcas.Any(pm => pm.MarcaId == marcaId.Value));

            if (activo.HasValue)
                query = query.Where(p => p.Activo == activo.Value);

            return query
                .OrderBy(p => p.Nombre)
                .Select(p => new ProveedorListaItem
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    PersonaContacto = p.PersonaContacto,
                    Telefono = p.Telefono,
                    Activo = p.Activo,
                    Marcas = p.Marcas.Select(pm => pm.Marca!.Nombre).OrderBy(n => n).ToList(),
                    UltimoAbastecimiento = p.Abastecimientos.Max(a => (DateTime?)a.Fecha)
                })
                .ToListAsync();
        }

        // Proveedores que pueden abastecer (el select del formulario "Abastecer tienda"), con sus marcas.
        public Task<List<Proveedor>> ListarActivosAsync() =>
            context.Proveedores.AsNoTracking()
                .Include(p => p.Marcas).ThenInclude(pm => pm.Marca)
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

        // Ficha del proveedor: sus marcas y su historial de abastecimientos (más reciente primero).
        public Task<Proveedor?> ObtenerAsync(int id) =>
            context.Proveedores.AsNoTracking()
                .Include(p => p.Marcas).ThenInclude(pm => pm.Marca)
                .Include(p => p.Abastecimientos.OrderByDescending(a => a.Fecha)).ThenInclude(a => a.Detalles)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id);

        // Datos del proveedor listos para el formulario de edición (null si no existe).
        public async Task<ProveedorFormViewModel?> ObtenerFormularioAsync(int id)
        {
            var proveedor = await context.Proveedores.AsNoTracking()
                .Include(p => p.Marcas)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (proveedor == null) return null;

            var model = new ProveedorFormViewModel
            {
                Id = proveedor.Id,
                Nombre = proveedor.Nombre,
                Nit = proveedor.Nit,
                PersonaContacto = proveedor.PersonaContacto,
                Telefono = proveedor.Telefono,
                Email = proveedor.Email,
                Ciudad = proveedor.Ciudad,
                Direccion = proveedor.Direccion,
                Notas = proveedor.Notas,
                MarcasSeleccionadas = proveedor.Marcas.Select(pm => pm.MarcaId).ToList()
            };
            await CargarMarcasDisponiblesAsync(model);
            return model;
        }

        // Llena los chips de marcas del formulario: las activas, más las inactivas que el proveedor
        // ya tenía (para no perderlas al editar). Se llama también al volver a mostrar el form con errores.
        public async Task CargarMarcasDisponiblesAsync(ProveedorFormViewModel model)
        {
            var seleccionadas = model.MarcasSeleccionadas;
            model.MarcasDisponibles = await context.Marcas.AsNoTracking()
                .Where(m => m.Activo || seleccionadas.Contains(m.Id))
                .OrderBy(m => m.Nombre)
                .Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = m.Nombre,
                    Selected = seleccionadas.Contains(m.Id)
                })
                .ToListAsync();
        }

        public async Task<ResultadoOperacion> CrearAsync(ProveedorFormViewModel model)
        {
            var proveedor = new Proveedor();
            CopiarDatos(model, proveedor);

            foreach (var marcaId in await MarcasValidasAsync(model.MarcasSeleccionadas))
                proveedor.Marcas.Add(new ProveedorMarca { MarcaId = marcaId });

            context.Proveedores.Add(proveedor);
            await context.SaveChangesAsync();
            return ResultadoOperacion.Ok(proveedor.Id);
        }

        // Actualiza los datos y reemplaza las marcas por las seleccionadas en el formulario.
        public async Task<ResultadoOperacion> ActualizarAsync(ProveedorFormViewModel model)
        {
            var proveedor = await context.Proveedores
                .Include(p => p.Marcas)
                .FirstOrDefaultAsync(p => p.Id == model.Id);
            if (proveedor == null)
                return ResultadoOperacion.Falla("El proveedor no existe.");

            CopiarDatos(model, proveedor);

            var nuevas = await MarcasValidasAsync(model.MarcasSeleccionadas);

            // Quitar las marcas que se desmarcaron...
            foreach (var vinculo in proveedor.Marcas.Where(pm => !nuevas.Contains(pm.MarcaId)).ToList())
                proveedor.Marcas.Remove(vinculo);

            // ...y agregar las que se marcaron recién.
            foreach (var marcaId in nuevas.Where(id => proveedor.Marcas.All(pm => pm.MarcaId != id)))
                proveedor.Marcas.Add(new ProveedorMarca { MarcaId = marcaId });

            await context.SaveChangesAsync();
            return ResultadoOperacion.Ok(proveedor.Id);
        }

        // Activar / desactivar. Un proveedor inactivo no aparece al abastecer, pero su historial sigue visible.
        public async Task<ResultadoOperacion> CambiarEstadoAsync(int id, bool activo)
        {
            var proveedor = await context.Proveedores.FindAsync(id);
            if (proveedor == null)
                return ResultadoOperacion.Falla("El proveedor no existe.");

            proveedor.Activo = activo;
            await context.SaveChangesAsync();
            return ResultadoOperacion.Ok(proveedor.Id);
        }

        // Nunca confiar en los ids que llegan del formulario: se quedan solo los de marcas que existen.
        private async Task<List<int>> MarcasValidasAsync(List<int> ids)
        {
            var distintos = ids.Distinct().ToList();
            return await context.Marcas
                .Where(m => distintos.Contains(m.Id))
                .Select(m => m.Id)
                .ToListAsync();
        }

        private static void CopiarDatos(ProveedorFormViewModel model, Proveedor proveedor)
        {
            proveedor.Nombre = model.Nombre.Trim();
            proveedor.Nit = Limpiar(model.Nit);
            proveedor.PersonaContacto = Limpiar(model.PersonaContacto);
            proveedor.Telefono = Limpiar(model.Telefono);
            proveedor.Email = Limpiar(model.Email);
            proveedor.Ciudad = Limpiar(model.Ciudad);
            proveedor.Direccion = Limpiar(model.Direccion);
            proveedor.Notas = Limpiar(model.Notas);
        }

        private static string? Limpiar(string? texto) =>
            string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }
}
