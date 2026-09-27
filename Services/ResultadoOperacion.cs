namespace EcommerceApp.Services
{
    // Resultado de una operación de los servicios de inventario (proveedores, marcas, abastecimientos).
    // Si algo no se puede hacer por una regla de negocio, se devuelve el mensaje para el usuario
    // en vez de lanzar una excepción (mismo criterio que ResultadoPedido en PedidoService).
    public record ResultadoOperacion(int Id, string? Error)
    {
        public bool Exito => Error == null;

        public static ResultadoOperacion Ok(int id = 0) => new(id, null);
        public static ResultadoOperacion Falla(string error) => new(0, error);
    }
}
