using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace TPF.RecuperacionTecnologica.Web.Services.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;

    public SmtpEmailSender(IOptions<SmtpSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        using var mensaje = new MailMessage
        {
            From = new MailAddress(_settings.CorreoRemitente),
            Subject = asunto,
            Body = cuerpoHtml,
            IsBodyHtml = true
        };
        mensaje.To.Add(destinatario);

        using var cliente = new SmtpClient(_settings.Host, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.Usuario, _settings.Password),
            EnableSsl = _settings.HabilitarSsl
        };

        // si falla el envío, la excepción sube a quien llamó para que decida qué hacer
        await cliente.SendMailAsync(mensaje);
    }
}
