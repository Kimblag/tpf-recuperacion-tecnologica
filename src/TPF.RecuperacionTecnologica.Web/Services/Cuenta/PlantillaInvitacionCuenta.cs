using System.Net;

namespace TPF.RecuperacionTecnologica.Web.Services.Cuenta;

public static class PlantillaInvitacionCuenta
{
    private const string Fuente = "'Segoe UI',Roboto,Helvetica,Arial,sans-serif";

    public static string Armar(string enlace, string nombreOrganizacion)
    {
        var organizacion = WebUtility.HtmlEncode(nombreOrganizacion);
        var enlaceHtml = WebUtility.HtmlEncode(enlace);

        return $$"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Activar la cuenta</title>
            </head>
            <body style="margin:0;padding:0;background-color:#F7F8F6;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#F7F8F6;">
                <tr>
                  <td align="center" style="padding:32px 16px;">
                    <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="width:100%;max-width:560px;">
                      <tr>
                        <td style="background-color:#FFFFFF;border:1px solid #D9DED7;border-radius:10px;padding:28px;">
                          <h1 style="margin:0 0 14px;font-family:{{Fuente}};font-size:20px;line-height:1.3;font-weight:600;color:#202426;">Activar la cuenta</h1>
                          <p style="margin:0 0 14px;font-family:{{Fuente}};font-size:14px;line-height:1.6;color:#202426;">Hola,</p>
                          <p style="margin:0 0 14px;font-family:{{Fuente}};font-size:14px;line-height:1.6;color:#202426;">{{organizacion}} abrió una cuenta a nombre de esta dirección de correo para registrar una solicitud de equipo. Para activarla, hay que elegir una contraseña con el botón.</p>
                          <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:6px 0 18px;">
                            <tr>
                              <td align="center" bgcolor="#557744" style="background-color:#557744;border-radius:8px;">
                                <a href="{{enlaceHtml}}" target="_blank" style="display:inline-block;padding:12px 22px;font-family:{{Fuente}};font-size:14px;font-weight:600;line-height:1;color:#FFFFFF;text-decoration:none;border-radius:8px;">Elegir contraseña</a>
                              </td>
                            </tr>
                          </table>
                          <p style="margin:0 0 20px;font-family:{{Fuente}};font-size:12px;line-height:1.5;color:#656D67;">Si el botón no funciona, copiar y pegar este enlace en el navegador:<br><a href="{{enlaceHtml}}" target="_blank" style="color:#476339;word-break:break-all;">{{enlaceHtml}}</a></p>
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
                            <tr>
                              <td style="background-color:#E5EEDC;border:1px solid #D9DED7;border-radius:8px;padding:12px 14px;font-family:{{Fuente}};font-size:13px;line-height:1.5;color:#202426;">El enlace vence en 1 hora y solo puede usarse una vez. Si venció, se puede pedir otro desde el inicio de sesión, con la opción "Olvidé mi contraseña". Si este correo no era esperado, se puede ignorar.</td>
                            </tr>
                          </table>
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
}
