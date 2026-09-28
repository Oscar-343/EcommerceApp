using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Formulario de la página "Contacto".
    public class ContactoViewModel
    {
        public static readonly string[] Motivos =
        {
            "Rutas y reservas",
            "Pedidos y productos",
            "Grupos y empresas",
            "Otro"
        };

        [Required(ErrorMessage = "Escribe tu nombre.")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe tu correo.")]
        [EmailAddress(ErrorMessage = "Ese correo no es válido.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Ese teléfono no es válido.")]
        [StringLength(30)]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "Elige un motivo.")]
        public string Motivo { get; set; } = "Rutas y reservas";

        [Required(ErrorMessage = "Escribe tu mensaje.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "El mensaje debe tener entre 10 y 2000 caracteres.")]
        public string Mensaje { get; set; } = string.Empty;
    }
}
