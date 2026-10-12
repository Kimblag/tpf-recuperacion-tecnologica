using Microsoft.AspNetCore.Identity;
using TPF.RecuperacionTecnologica.Web.ViewModels.Personal;

namespace TPF.RecuperacionTecnologica.Web.Services.Personal;

public interface IPersonalService
{
    /// <summary>
    /// Da de alta a un integrante del Personal. Crea su cuenta de Identity con el rol de Identity
    /// correspondiente a <c>RolAsignado</c> y su registro de Personal (Activo), y deja constancia en la auditoría.
    /// </summary>
    /// <param name="modelo">Datos del nuevo integrante.</param>
    /// <param name="codigoAdministrador">Código del Administrador que realiza el alta.</param>
    /// <returns>
    /// Éxito, o los errores (DNI o correo ya registrados, contraseña inválida, etc.)
    /// sin que haya quedado ninguna cuenta ni registro creado.
    /// </returns>
    Task<IdentityResult> AltaAsync(AltaPersonalViewModel modelo, int codigoAdministrador);

    /// <summary>
    /// Devuelve el código de Personal asociado a una cuenta de Identity, o null si no corresponde a Personal.
    /// </summary>
    Task<int?> ObtenerCodigoPersonalAsync(string identityUserId);
}
