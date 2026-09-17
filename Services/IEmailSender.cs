namespace EcommerceApp.Services
{
    // Abstracción mínima para enviar correos.
    // La implementación actual usa SMTP (o imprime en la consola si no hay SMTP).
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}