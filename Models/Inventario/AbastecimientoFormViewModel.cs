using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models.Inventario
{
    // Formulario "Abastecer tienda". Solo trae lo que elige el admin:
    // los subtotales, el total y RegistradoPor los calcula el servidor.
    public class AbastecimientoFormViewModel
    {
        // [Required] no sirve con un int (siempre tiene valor): se exige un Id válido con Range.
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un proveedor")]
        public int ProveedorId { get; set; }

        [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string? NumeroComprobante { get; set; }

        [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string? Observaciones { get; set; }

        // Filas del formulario dinámico: llegan como Lineas[0].ProductoId, Lineas[0].Cantidad, ...
        public List<LineaAbastecimiento> Lineas { get; set; } = new();
    }

    // Un producto dentro del abastecimiento.
    public class LineaAbastecimiento
    {
        [Range(1, int.MaxValue, ErrorMessage = "Producto inválido")]
        public int ProductoId { get; set; }

        [Range(1, 10000, ErrorMessage = "Cantidad inválida")]
        public int Cantidad { get; set; }

        // Costo de compra al proveedor, no el precio de venta.
        [Range(0.01, 1000000, ErrorMessage = "Costo inválido")]
        public decimal CostoUnitario { get; set; }
    }
}
