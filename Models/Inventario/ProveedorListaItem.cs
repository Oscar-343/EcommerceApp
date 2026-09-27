namespace EcommerceApp.Models.Inventario
{
    // Una fila del listado de proveedores (Proveedores/Index).
    public class ProveedorListaItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? PersonaContacto { get; set; }
        public string? Telefono { get; set; }

        // Nombres de las marcas que distribuye (se muestran como chips).
        public List<string> Marcas { get; set; } = new();

        // Fecha del abastecimiento más reciente; null si todavía no entregó nada.
        public DateTime? UltimoAbastecimiento { get; set; }

        public bool Activo { get; set; }
    }
}
