namespace EcommerceApp.Models
{
    // Tabla puente (muchos a muchos): qué marcas distribuye cada proveedor.
    // Clave primaria compuesta: (ProveedorId, MarcaId).
    public class ProveedorMarca
    {
        public int ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }

        public int MarcaId { get; set; }
        public Marca? Marca { get; set; }
    }
}
