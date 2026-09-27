using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Ingreso de mercadería a la tienda: qué proveedor entregó qué productos.
    // Al registrarse suma stock. No se edita ni se borra, así el historial y el stock siempre coinciden.
    public class Abastecimiento
    {
        public int Id { get; set; }

        public int ProveedorId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        // Nota de entrega o factura del proveedor
        [StringLength(50)]
        public string? NumeroComprobante { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // Suma de los subtotales de los detalles; se calcula en el servidor.
        public decimal Total { get; set; }

        // Usuario admin que lo registró (sale de User.Identity.Name, nunca del formulario).
        [StringLength(256)]
        public string RegistradoPor { get; set; } = string.Empty;

        // Navegación
        public Proveedor? Proveedor { get; set; }
        public ICollection<DetalleAbastecimiento> Detalles { get; set; } = new List<DetalleAbastecimiento>();
    }
}
