using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Marca de los productos (reemplaza al texto libre Product.Brand).
    // No se borra: se desactiva, así los productos y proveedores que la usan no quedan huérfanos.
    public class Marca
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Nombre { get; set; } = string.Empty;

        // Logo opcional, subido a Supabase Storage igual que las imágenes de productos.
        public string? LogoUrl { get; set; }

        public bool Activo { get; set; } = true;

        // Navegación
        public ICollection<Product> Productos { get; set; } = new List<Product>();
        public ICollection<ProveedorMarca> Proveedores { get; set; } = new List<ProveedorMarca>();
    }
}
