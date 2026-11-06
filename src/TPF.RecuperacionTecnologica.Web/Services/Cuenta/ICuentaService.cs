using Microsoft.AspNetCore.Identity;

namespace TPF.RecuperacionTecnologica.Web.Services.Cuenta;

public interface ICuentaService
{
    /// <summary>
    /// Verifica las credenciales y el estado de la cuenta, y recién entonces inicia la sesión.
    /// </summary>
    Task<ResultadoInicioSesion> IniciarSesionAsync(string correoElectronico, string password, bool recordarSesion);

    /// <summary>
    /// Cierra la sesión y audita el cierre de la cuenta indicada.
    /// </summary>
    Task CerrarSesionAsync(string? identityUserId);

    /// <summary>
    /// Regenera la sesión actual para que refleje los datos vigentes de la cuenta, sin pedir iniciar sesión de nuevo.
    /// </summary>
    Task RefrescarSesionAsync(string identityUserId);

    /// <param name="construirRuta">Arma la ruta relativa del enlace a partir del id del usuario y el token ya codificado.</param>
    /// <param name="urlBaseSolicitud">Dominio de la solicitud actual (por ejemplo https://localhost:7127), este solo se usa en desarrollo, ya queen producción el dominio sale de la configuración.</param>
    Task SolicitarRestablecimientoAsync(string correoElectronico, Func<string, string, string> construirRuta, string urlBaseSolicitud);

    Task EnviarInvitacionAsync(string identityUserId, Func<string, string, string> construirRuta, string urlBaseSolicitud);

    /// <summary>
    /// Vuelve a enviar la invitación a una cuenta que todavía no eligió su contraseña, con un token nuevo.
    /// Se permite una vez cada 2 minutos por cuenta.
    /// </summary>
    Task<ResultadoReenvioInvitacion> ReenviarInvitacionAsync(int nroUsuario, Func<string, string, string> construirRuta, string urlBaseSolicitud);

    /// <summary>
    /// Restablece la contraseña con el token recibido por correo. Si el enlace no es válido,
    /// devuelve siempre el mismo error. Al restablecer también se levanta el bloqueo por intentos fallidos.
    /// </summary>
    Task<IdentityResult> RestablecerContrasenaAsync(string idUsuario, string token, string nuevaContrasena);

    /// <summary>
    /// Activa una cuenta invitada: verifica el token de la invitación, guarda la primera contraseña,
    /// confirma el correo y audita la activación. Si el enlace venció, no es válido o la cuenta ya tiene
    /// contraseña, devuelve siempre el mismo error.
    /// </summary>
    Task<IdentityResult> ActivarCuentaAsync(string idUsuario, string token, string contrasena);

    /// <summary>
    /// Indica si el enlace de invitación todavía sirve, para avisar antes de que la persona complete el formulario.
    /// </summary>
    Task<bool> EsEnlaceActivacionValidoAsync(string idUsuario, string token);

    /// <summary>
    /// Cambia la contraseña de la cuenta con la sesión iniciada. Cierra las demás sesiones abiertas
    /// y mantiene la actual.
    /// </summary>
    Task<IdentityResult> CambiarContrasenaAsync(string identityUserId, string contrasenaActual, string contrasenaNueva);
}