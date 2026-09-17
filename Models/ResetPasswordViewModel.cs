using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Vista-modelo para escribir la contraseña nueva.
    // El correo y el token llegan ocultos desde el enlace del correo.
    public class ResetPasswordViewModel
    {
        [Required, EmailAddress, Display(Name = "Correo")]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password), Display(Name = "Contraseña nueva")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password), Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Token de restablecimiento generado por Identity (viene en el enlace).
        public string Token { get; set; } = string.Empty;
    }
}