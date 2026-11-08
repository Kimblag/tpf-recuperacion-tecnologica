using System.Net;

namespace TPF.RecuperacionTecnologica.Web.Services.Notificaciones;

/// <summary>
/// Cuerpos HTML de los avisos a la persona solicitante. Comparten el diseño de las plantillas de cuenta
/// y los textos están en forma neutral e impersonal.
/// </summary>
public static class PlantillaNotificacion
{
    private const string Fuente = "'Segoe UI',Roboto,Helvetica,Arial,sans-serif";

    public static string Asignacion(
        string organizacion, string solicitante, int nroSolicitud, string equipo, DateTime fechaLimite, string? enlace) =>
        Armar(
            organizacion, "Equipo asignado", solicitante,
            [
                $"Se asignó el equipo <strong>{Codificar(equipo)}</strong> a la solicitud <strong>{nroSolicitud}</strong>.",
                $"Podés retirar el equipo hasta el <strong>{fechaLimite:dd/MM/yyyy}</strong>, inclusive. Para retirarlo, presentate en la sede con el documento de identidad de la persona titular de la solicitud."
            ],
            "Pasado ese día, la asignación se libera y el equipo queda disponible para otras personas.",
            enlace, "Ver mis solicitudes");

    public static string VencimientoProximo(
        string organizacion, string solicitante, int nroSolicitud, string equipo, DateTime fechaLimite, string? enlace) =>
        Armar(
            organizacion, "El plazo de retiro está por vencer", solicitante,
            [
                $"El plazo para retirar el equipo <strong>{Codificar(equipo)}</strong>, asignado a la solicitud <strong>{nroSolicitud}</strong>, vence el <strong>{fechaLimite:dd/MM/yyyy}</strong>."
            ],
            "Si el equipo no se retira hasta ese día, la asignación se libera y la solicitud se cierra.",
            enlace, "Ver mis solicitudes");

    public static string Liberacion(
        string organizacion, string solicitante, int nroSolicitud, string equipo, string motivo, string? enlace) =>
        Armar(
            organizacion, "Solicitud cerrada", solicitante,
            [
                $"Se liberó la asignación del equipo <strong>{Codificar(equipo)}</strong> y la solicitud <strong>{nroSolicitud}</strong> quedó cerrada.",
                $"Motivo: {Codificar(motivo)}"
            ],
            "Si el equipo sigue siendo necesario, se puede registrar una nueva solicitud.",
            enlace, "Ver mis solicitudes");

    public static string CierrePorEntrega(
        string organizacion, string solicitante, int nroSolicitud, string equipo, string? enlace) =>
        Armar(
            organizacion, "Solicitud cerrada", solicitante,
            [
                $"El equipo <strong>{Codificar(equipo)}</strong> se entregó a otra persona y ya no está disponible, por lo que la solicitud <strong>{nroSolicitud}</strong> quedó cerrada."
            ],
            "Se puede registrar una nueva solicitud para otro de los equipos disponibles.",
            enlace, "Ver mis solicitudes");

    public static string CierrePorBaja(
        string organizacion, string solicitante, int nroSolicitud, string equipo, string? enlace) =>
        Armar(
            organizacion, "Solicitud cerrada", solicitante,
            [
                $"El equipo <strong>{Codificar(equipo)}</strong> no se pudo recuperar y ya no se puede asignar, por lo que la solicitud <strong>{nroSolicitud}</strong> quedó cerrada."
            ],
            "Podés registrar una nueva solicitud para otro de los equipos disponibles.",
            enlace, "Ver mis solicitudes");

    private static string Codificar(string texto) => WebUtility.HtmlEncode(texto);

    private static string Armar(
        string nombreOrganizacion, string titulo, string solicitante, string[] parrafos,
        string nota, string? enlace, string textoBoton)
    {
        var organizacion = Codificar(nombreOrganizacion);
        var cuerpo = string.Concat(parrafos.Select(p =>
            $"""<p style="margin:0 0 14px;font-family:{Fuente};font-size:14px;line-height:1.6;color:#202426;">{p}</p>"""));

        var boton = string.IsNullOrWhiteSpace(enlace) ? string.Empty : $"""
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:6px 0 18px;">
              <tr>
                <td align="center" bgcolor="#557744" style="background-color:#557744;border-radius:8px;">
                  <a href="{Codificar(enlace)}" target="_blank" style="display:inline-block;padding:12px 22px;font-family:{Fuente};font-size:14px;font-weight:600;line-height:1;color:#FFFFFF;text-decoration:none;">{Codificar(textoBoton)}</a>
                </td>
              </tr>
            </table>
            """;

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{Codificar(titulo)}</title>
            </head>
            <body style="margin:0;padding:0;background-color:#F7F8F6;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#F7F8F6;">
                <tr>
                  <td align="center" style="padding:32px 16px;">
                    <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="width:100%;max-width:560px;">
                      <tr>
                        <td style="background-color:#FFFFFF;border:1px solid #D9DED7;border-radius:10px;padding:28px;">
                          <h1 style="margin:0 0 14px;font-family:{Fuente};font-size:20px;line-height:1.3;font-weight:600;color:#202426;">{Codificar(titulo)}</h1>
                          <p style="margin:0 0 14px;font-family:{Fuente};font-size:14px;line-height:1.6;color:#202426;">Hola, {Codificar(solicitante)}:</p>
                          {cuerpo}
                          {boton}
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
                            <tr>
                              <td style="background-color:#E5EEDC;border:1px solid #D9DED7;border-radius:8px;padding:12px 14px;font-family:{Fuente};font-size:13px;line-height:1.5;color:#202426;">{Codificar(nota)}</td>
                            </tr>
                          </table>
                          <p style="margin:18px 0 0;font-family:{Fuente};font-size:12px;line-height:1.5;color:#656D67;">{organizacion}</p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    public static string EntregaAlDonante(
        string organizacion, string donante, int nroEquipo, string equipo, string destinatario, DateTime fechaEntrega, string? enlace) =>
        Armar(
            organizacion, "Tu equipo fue entregado", donante,
            [
                $"El equipo <strong>{Codificar(equipo)}</strong> (equipo Nº <strong>{nroEquipo}</strong>), " +
                $"que donaste, ya tiene destino: el <strong>{fechaEntrega:dd/MM/yyyy}</strong> se entregó a {Codificar(destinatario)}.",
                "Gracias por tu aporte: gracias a donaciones como la tuya, más personas pueden acceder a tecnología."
            ],
            "Podés ver todas las etapas del equipo desde Mis donaciones.",
            enlace, "Ver mis donaciones");
}