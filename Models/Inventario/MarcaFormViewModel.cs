using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models.Inventario
{
    // Formulario de crear/editar marca. Id es null al crear.
    public class MarcaFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(80, ErrorMessage = "Máximo 80 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        // URL del logo en Supabase Storage (opcional). [Url] exige que sea http(s) o ftp:
        // así no se guarda algo como "javascript:..." que luego se pinte en un src.
        [Url(ErrorMessage = "Ingresa una URL válida (https://...)")]
        [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string? LogoUrl { get; set; }
    }
}
