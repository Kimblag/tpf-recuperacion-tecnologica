using Microsoft.AspNetCore.Identity;
using TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;

namespace TPF.RecuperacionTecnologica.Web.Services.Usuarios;

public interface IUsuarioService
{
    /// <summary>
    /// Permite registrar a un Usuario nuevo. Crea su cuenta de Identity y su perfil de dominio,
    /// y deja constancia en la auditoría.
    /// </summary>
    /// <returns>
    /// Éxito, o los errores de validación o de Identity sin que se haya creado el Usuario.
    /// </returns>
    Task<IdentityResult> RegistrarAsync(RegistroUsuarioViewModel modelo);
}
