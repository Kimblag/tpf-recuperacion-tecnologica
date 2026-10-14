using Microsoft.AspNetCore.Identity;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;
using TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

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

    /// <summary>
    /// Lista los Usuarios registrados, ordenados por número de usuario, con filtros opcionales.
    /// </summary>
    /// <param name="tipo">Si se indica, solo los Usuarios de ese tipo.</param>
    /// <param name="estado">Si se indica, solo los Usuarios en ese estado.</param>
    Task<UsuarioListadoViewModel> ListarAsync(TipoUsuario? tipo, EstadoUsuario? estado);

    /// <summary>
    /// Devuelve el detalle de un Usuario, o null si no existe.
    /// </summary>
    Task<UsuarioDetalleViewModel?> ObtenerDetalleAsync(int nroUsuario);
}
