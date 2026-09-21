using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Reserva de un servicio/ruta por parte de un usuario para una fecha de salida concreta.
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public int ServiceId { get; set; }

        // Fecha en la que el usuario sale a la ruta (no la fecha en que reservó).
        public DateOnly TripDate { get; set; }

        // Fecha en la que se hizo la reserva.
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Range(1, 50, ErrorMessage = "Debe ser entre 1 y 50 personas.")]
        public int PeopleCount { get; set; } = 1;

        public decimal UnitPrice { get; set; }

        // Precio total = personas * precio unitario (se calcula al reservar).
        public decimal TotalPrice { get; set; }

        // Estado: Pendiente, Recorrido, Acabado, Cancelado.
        [MaxLength(20), RegularExpression(@"^(Pendiente|Recorrido|Acabado|Cancelado)$", ErrorMessage = "Estado no válido.")]
        public string Status { get; set; } = "Pendiente";

        // Se llena cuando el estado pasa a Acabado (fecha del ingreso, usada en reportes).
        public DateTime? CompletedAt { get; set; }

        // Navegación (sin colecciones en los extremos para mantenerlo simple).
        public ApplicationUser? User { get; set; }
        public Service? Service { get; set; }
    }
}
