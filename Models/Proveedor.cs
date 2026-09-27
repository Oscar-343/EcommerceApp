using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Proveedor que abastece la tienda. Solo lo gestiona el administrador (no tiene login).
    // No se borra: se desactiva, para conservar su historial de abastecimientos.
    public class Proveedor
    {
        public int Id { get; set; }

        // Razón social o nombre comercial
        [Required, StringLength(120)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Nit { get; set; }

        [StringLength(100)]
        public string? PersonaContacto { get; set; }

        [StringLength(30)]
        public string? Telefono { get; set; }

        [StringLength(120), EmailAddress]
        public string? Email { get; set; }

        [StringLength(80)]
        public string? Ciudad { get; set; }

        [StringLength(200)]
        public string? Direccion { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        // Desactivar en vez de borrar: un proveedor inactivo no aparece al abastecer.
        public bool Activo { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Navegación
        public ICollection<ProveedorMarca> Marcas { get; set; } = new List<ProveedorMarca>();
        public ICollection<Abastecimiento> Abastecimientos { get; set; } = new List<Abastecimiento>();
    }
}
