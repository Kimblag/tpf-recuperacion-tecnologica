using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace TPF.RecuperacionTecnologica.Web.Services.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;


    public SmtpEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        ValidarConfiguracion();

        using var mensaje = new MailMessage
        {
            From = new MailAddress(_settings.CorreoRemitente),
            Subject = asunto,
            SubjectEncoding = Encoding.UTF8,
            Body = cuerpoHtml,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };
        mensaje.To.Add(destinatario);

        using var cliente = new SmtpClient(_settings.Host, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.Usuario, _settings.Password),
            EnableSsl = _settings.HabilitarSsl
        };

        // agrego tiempo de espera para que no quede colgado si el servidor SMTP no responde
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.TimeoutSegundos));

         // si falla el envío, la excepción sube a quien llamó para que decida qué hacer
        try
        {
            await cliente.SendMailAsync(mensaje, limite.Token);
        }
        catch (OperationCanceledException) when (limite.IsCancellationRequested)
        {

            _logger.LogError("Se agotó el tiempo de espera ({Segundos} s) al enviar un correo", _settings.TimeoutSegundos);
            throw new TimeoutException("El servidor de correo no respondió a tiempo.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el envío de correo a {Destinatario}", destinatario);
            throw;
        }

    }

    private void ValidarConfiguracion()
    {
        var faltantes = new List<string>();

        if (string.IsNullOrWhiteSpace(_settings.Host)) faltantes.Add("Smtp:Host");
        if (_settings.Port <= 0) faltantes.Add("Smtp:Port");
        if (string.IsNullOrWhiteSpace(_settings.Usuario)) faltantes.Add("Smtp:Usuario");
        if (string.IsNullOrWhiteSpace(_settings.Password)) faltantes.Add("Smtp:Password");
        if (string.IsNullOrWhiteSpace(_settings.CorreoRemitente)) faltantes.Add("Smtp:CorreoRemitente");

        if (faltantes.Count > 0)
        {
            throw new InvalidOperationException(
                $"Falta configurar el envío de correo: {string.Join(", ", faltantes)}. Cargar los valores con user-secrets.");
        }
    }
}
