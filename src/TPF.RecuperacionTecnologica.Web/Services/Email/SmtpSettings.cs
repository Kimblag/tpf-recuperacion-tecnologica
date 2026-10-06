namespace TPF.RecuperacionTecnologica.Web.Services.Email;

/// <summary>
/// Datos de conexión al servidor SMTP. Se cargan desde la sección "Smtp" de la configuración;
/// los valores reales se guardan en user-secrets, nunca en archivos versionados.
/// </summary>
public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string Usuario { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool HabilitarSsl { get; set; }

    public string CorreoRemitente { get; set; } = string.Empty;
}
