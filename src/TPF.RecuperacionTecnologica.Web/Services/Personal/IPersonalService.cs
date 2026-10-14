using Microsoft.AspNetCore.Identity;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
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
    /// Lista el Personal registrado, ordenado por código, con filtros opcionales.
    /// </summary>
    /// <param name="busqueda">Texto a buscar en nombre y apellido, DNI o código (por ejemplo "PER-0002" o "2").</param>
    /// <param name="rol">Si se indica, solo el Personal con ese rol.</param>
    /// <param name="estado">Si se indica, solo el Personal en ese estado.</param>
    Task<PersonalListadoViewModel> ListarAsync(string? busqueda, RolPersonal? rol, EstadoPersonal? estado);

    /// <summary>
    /// Devuelve el código de Personal asociado a una cuenta de Identity, o null si no corresponde a Personal.
    /// </summary>
    Task<int?> ObtenerCodigoPersonalAsync(string identityUserId);

    Task<PersonalDetalleViewModel?> ObtenerDetalleAsync(int codigoPersonal);
    Task<IdentityResult> EditarAsync(EdicionPersonalViewModel modelo, int codigoAdministrador);
}
