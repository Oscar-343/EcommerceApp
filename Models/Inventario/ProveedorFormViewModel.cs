using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EcommerceApp.Models.Inventario
{
    // Formulario de crear/editar proveedor. Id es null al crear.
    public class ProveedorFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(120, ErrorMessage = "Máximo 120 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Máximo 20 caracteres")]
        public string? Nit { get; set; }

        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string? PersonaContacto { get; set; }

        [StringLength(30, ErrorMessage = "Máximo 30 caracteres")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El email no es válido")]
        [StringLength(120, ErrorMessage = "Máximo 120 caracteres")]
        public string? Email { get; set; }

        [StringLength(80, ErrorMessage = "Máximo 80 caracteres")]
        public string? Ciudad { get; set; }

        [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
        public string? Direccion { get; set; }

        [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string? Notas { get; set; }

        // Ids de las marcas marcadas en el formulario (chips seleccionables).
        public List<int> MarcasSeleccionadas { get; set; } = new();

        // Solo para pintar los chips; no llega en el POST, por eso no se valida.
        [ValidateNever]
        public List<SelectListItem> MarcasDisponibles { get; set; } = new();
    }
}
