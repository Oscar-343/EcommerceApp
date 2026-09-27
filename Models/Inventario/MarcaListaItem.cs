namespace EcommerceApp.Models.Inventario
{
    // Una tarjeta del listado de marcas (Marcas/Index), con sus conteos ya calculados.
    public class MarcaListaItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public bool Activo { get; set; }
        public int CantidadProductos { get; set; }
        public int CantidadProveedores { get; set; }
    }
}
