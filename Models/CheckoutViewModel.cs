using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EcommerceApp.Models
{
    // Formulario del checkout: datos de entrega y método de pago elegido.
    // Nunca contiene datos de tarjeta: solo la marca y los últimos 4 dígitos, para mostrarlos.
    public class CheckoutViewModel
    {
        // Datos de entrega / contacto
        [Required(ErrorMessage = "Ingresa tu nombre completo")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string NombreCompleto { get; set; } = "";

        [Required(ErrorMessage = "Ingresa tu teléfono")]
        [Phone(ErrorMessage = "Ingresa un teléfono válido")]
        [StringLength(30, ErrorMessage = "Máximo 30 caracteres")]
        public string Telefono { get; set; } = "";

        [Required(ErrorMessage = "Ingresa tu email")]
        [EmailAddress(ErrorMessage = "Ingresa un email válido")]
        [StringLength(256, ErrorMessage = "Máximo 256 caracteres")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Ingresa la ciudad")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string Ciudad { get; set; } = "";

        [Required(ErrorMessage = "Ingresa la dirección")]
        [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
        public string Direccion { get; set; } = "";

        [StringLength(300, ErrorMessage = "Máximo 300 caracteres")]
        public string? Referencia { get; set; }

        // Pago (solo lo que se puede mostrar)
        [Required(ErrorMessage = "Elige un método de pago")]
        public string MetodoPago { get; set; } = "Tarjeta";   // Tarjeta | QR | Transferencia

        public string? TarjetaMarca { get; set; }             // "VISA", "MASTERCARD"... (solo para mostrar)
        public string? TarjetaUltimos4 { get; set; }          // "4242" (solo para mostrar; el servidor lo revalida)

        // Resumen: se llena en el servidor, nunca desde el formulario
        // (BindNever: aunque alguien envíe campos "Resumen.*", se ignoran).
        [BindNever, ValidateNever]
        public ResumenCompraViewModel Resumen { get; set; } = new();
    }
}
