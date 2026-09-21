namespace EcommerceApp.Models
{
    // Tabla puente: qué productos se recomiendan como equipamiento para una ruta.
    // Clave primaria compuesta: (ServiceId, ProductId).
    public class ServiceProduct
    {
        public int ServiceId { get; set; }
        public Service? Service { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
