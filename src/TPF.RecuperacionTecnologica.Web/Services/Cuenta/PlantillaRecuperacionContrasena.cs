using System.Net;

namespace TPF.RecuperacionTecnologica.Web.Services.Cuenta;

/// <summary>
/// HTML del correo de recuperación de contraseña.
/// </summary>
public static class PlantillaRecuperacionContrasena
{
    private const string DescripcionPlataforma = "Plataforma para la Recuperación y Redistribución de Equipos Tecnológicos";

    // fuentes del sistema
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
              <meta name="color-scheme" content="light dark">
              <meta name="supported-color-schemes" content="light dark">
              <title>Restablecer su contraseña</title>
              <style>
                @media (prefers-color-scheme: dark) {
                  .fondo { background-color: #0D0F10 !important; }
                  .tarjeta { background-color: #202426 !important; border-color: #2E3335 !important; }
                  .texto { color: #F2F2F0 !important; }
                  .texto-sec { color: #A4A7A5 !important; }
                  .marca { background-color: #91BE68 !important; color: #0D0F10 !important; }
                  .boton { background-color: #91BE68 !important; }
                  .boton-enlace { color: #0D0F10 !important; }
                  .enlace { color: #91BE68 !important; }
                  .aviso { background-color: #2E3730 !important; border-color: #2E3335 !important; color: #F2F2F0 !important; }
                }
              </style>
            </head>
            <body class="fondo" style="margin:0;padding:0;background-color:#F7F8F6;">
              <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">Use este enlace para elegir una contraseña nueva. Vence en 1 hora.</div>
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" class="fondo" style="background-color:#F7F8F6;">
                <tr>
                  <td align="center" style="padding:32px 16px;">
                    <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="width:100%;max-width:560px;">
                      <tr>
                        <td align="center" style="padding-bottom:20px;">
                          <table role="presentation" cellpadding="0" cellspacing="0" border="0">
                            <tr>
                              <td class="marca" width="28" height="28" align="center" style="width:28px;height:28px;background-color:#557744;color:#FFFFFF;border-radius:6px;font-family:{{Fuente}};font-size:14px;font-weight:600;line-height:28px;">R</td>
                              <td class="texto" style="padding-left:10px;font-family:{{Fuente}};font-size:16px;font-weight:600;color:#202426;">{{organizacion}}</td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                      <tr>
                        <td class="tarjeta" style="background-color:#FFFFFF;border:1px solid #D9DED7;border-radius:10px;padding:28px;">
                          <h1 class="texto" style="margin:0 0 14px;font-family:{{Fuente}};font-size:20px;line-height:1.3;font-weight:600;color:#202426;">Restablecer su contraseña</h1>
                          <p class="texto" style="margin:0 0 14px;font-family:{{Fuente}};font-size:14px;line-height:1.6;color:#202426;">Hola,</p>
                          <p class="texto" style="margin:0 0 14px;font-family:{{Fuente}};font-size:14px;line-height:1.6;color:#202426;">Recibimos una solicitud para restablecer la contraseña de su cuenta. Para elegir una nueva, presione el botón.</p>
                          <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:6px 0 18px;">
                            <tr>
                              <td class="boton" align="center" bgcolor="#557744" style="background-color:#557744;border-radius:8px;">
                                <a class="boton-enlace" href="{{enlaceHtml}}" target="_blank" style="display:inline-block;padding:12px 22px;font-family:{{Fuente}};font-size:14px;font-weight:600;line-height:1;color:#FFFFFF;text-decoration:none;border-radius:8px;">Restablecer contraseña</a>
                              </td>
                            </tr>
                          </table>
                          <p class="texto-sec" style="margin:0 0 20px;font-family:{{Fuente}};font-size:12px;line-height:1.5;color:#656D67;">Si el botón no funciona, copie y pegue este enlace en su navegador:<br><a class="enlace" href="{{enlaceHtml}}" target="_blank" style="color:#476339;word-break:break-all;">{{enlaceHtml}}</a></p>
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
                            <tr>
                              <td class="aviso" style="background-color:#E5EEDC;border:1px solid #D9DED7;border-radius:8px;padding:12px 14px;font-family:{{Fuente}};font-size:13px;line-height:1.5;color:#202426;">El enlace vence en 1 hora y solo puede usarse una vez. Si usted no lo solicitó, ignore este correo: su contraseña no cambiará.</td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                      <tr>
                        <td align="center" class="texto-sec" style="padding-top:20px;font-family:{{Fuente}};font-size:12px;line-height:1.5;color:#656D67;">
                          {{organizacion}}<br>{{DescripcionPlataforma}}
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