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

    /// <param name="construirRuta">Arma la ruta relativa del enlace a partir del id del usuario y el token ya codificado.</param>
    /// <param name="urlBaseSolicitud">Dominio de la solicitud actual (por ejemplo https://localhost:7127), este solo se usa en desarrollo, ya queen producción el dominio sale de la configuración.</param>
    Task SolicitarRestablecimientoAsync(string correoElectronico, Func<string, string, string> construirRuta, string urlBaseSolicitud);

    /// <summary>
    /// Restablece la contraseña con el token recibido por correo. Si el enlace no es válido,
    /// devuelve siempre el mismo error. Al restablecer también se levanta el bloqueo por intentos fallidos.
    /// </summary>
    Task<IdentityResult> RestablecerContrasenaAsync(string idUsuario, string token, string nuevaContrasena);
}