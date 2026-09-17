using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Reserva de un servicio/ruta por parte de un usuario.
    // Clave primaria compuesta: (UserId, ServiceId).
    public class Reservation
    {
        public string UserId { get; set; } = string.Empty;

        public int ServiceId { get; set; }

        // Fecha en la que se hizo la reserva.
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Range(1, 50)]
        public int PeopleCount { get; set; } = 1;

        public decimal UnitPrice { get; set; }

        // Precio total = personas * precio unitario (se calcula al reservar).
        public decimal TotalPrice { get; set; }

        // Estado: Pendiente, Recorrido, Acabado, Cancelado.
        [MaxLength(20)]
        public string Status { get; set; } = "Pendiente";

        // Navegación (sin colecciones en los extremos para mantenerlo simple).
        public ApplicationUser? User { get; set; }
        public Service? Service { get; set; }
    }
}