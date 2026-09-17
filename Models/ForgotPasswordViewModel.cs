using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Vista-modelo para pedir el correo de recuperación
    // ("¿Olvidaste tu contraseña?").
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress, Display(Name = "Correo")]
        public string Email { get; set; } = string.Empty;
    }
}