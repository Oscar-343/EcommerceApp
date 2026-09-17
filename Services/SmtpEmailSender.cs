using System.Net;
using System.Net.Mail;

namespace EcommerceApp.Services
{
    // Envía correos por SMTP usando la sección "Email" de la configuración.
    // Si el Host está vacío (por ejemplo en desarrollo), imprime el mensaje
    // en la consola para poder probar el flujo sin servidor de correo.
    public class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var host = configuration["Email:Host"];

            // Sin SMTP configurado: el "correo" se muestra en la consola.
            if (string.IsNullOrWhiteSpace(host))
            {
                logger.LogInformation("EMAIL (SMTP no configurado) -> Para: {Email} | Asunto: {Subject} | Mensaje: {Message}",
                    email, subject, htmlMessage);
                return;
            }

            var port = configuration.GetValue<int>("Email:Port", 587);
            var username = configuration["Email:Username"] ?? "";
            var password = configuration["Email:Password"] ?? "";
            var from = configuration["Email:From"] ?? "no-reply@trenalsur.com";

#pragma warning disable SYSLIB0037 // SmtpClient es obsoleto pero sigue disponible y es la opción sin dependencias extra.
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(username, password)
            };
#pragma warning restore SYSLIB0037

            var message = new MailMessage(from, email, subject, htmlMessage) { IsBodyHtml = true };
            await client.SendMailAsync(message);
        }
    }
}