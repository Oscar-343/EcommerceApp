namespace EcommerceApp.Services
{
    // Pago SIMULADO: no cobra dinero ni recibe datos de tarjeta, solo imita la respuesta de una pasarela.
    // Siempre aprueba si el método y el monto son válidos.
    public class PagoSimuladoService : IPagoService
    {
        private static readonly string[] MetodosValidos = { "Tarjeta", "QR", "Transferencia" };

        public async Task<ResultadoPago> ProcesarAsync(string metodoPago, decimal monto)
        {
            if (!MetodosValidos.Contains(metodoPago))
                return new ResultadoPago(false, "", "Método de pago no válido.");

            if (monto <= 0)
                return new ResultadoPago(false, "", "El monto no es válido.");

            await Task.Delay(1200); // simula la espera de una pasarela

            // Código de ejemplo: TAS-20260926-4821 (fecha UTC + 4 dígitos al azar).
            var codigo = $"TAS-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 10000)}";
            return new ResultadoPago(true, codigo, "Pago simulado aprobado.");
        }
    }
}
