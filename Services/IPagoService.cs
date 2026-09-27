namespace EcommerceApp.Services
{
    // Abstracción del pago. Hoy lo implementa PagoSimuladoService; si algún día se integra
    // una pasarela real, solo se crea otra clase que implemente esta interfaz.
    public interface IPagoService
    {
        Task<ResultadoPago> ProcesarAsync(string metodoPago, decimal monto);
    }

    // Respuesta del pago: si salió bien, el código de transacción y un mensaje para mostrar.
    public record ResultadoPago(bool Exitoso, string CodigoTransaccion, string Mensaje);
}
