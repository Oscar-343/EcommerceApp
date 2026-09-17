using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Vista-modelo para completar el registro de una cuenta externa
    // (Google/GitHub): el proveedor ya aporta el email y falta el nombre.
    public class ExternalRegisterViewModel
    {
        [Required, EmailAddress, Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required, Display(Name = "Nombre completo")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Dirección")]
        public string? Address { get; set; }

        public string? ReturnUrl { get; set; }
    }
}