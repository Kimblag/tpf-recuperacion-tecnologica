namespace TPF.RecuperacionTecnologica.Web.Services.Email;

public interface IEmailSender
{
    /// <summary>
    /// Envía un correo electrónico con cuerpo en formato HTML.
    /// </summary>
    /// <param name="destinatario">Dirección de correo de quien lo recibe.</param>
    /// <param name="asunto">Asunto del correo.</param>
    /// <param name="cuerpoHtml">Contenido del correo en HTML.</param>
    Task SendEmailAsync(string destinatario, string asunto, string cuerpoHtml);
}
