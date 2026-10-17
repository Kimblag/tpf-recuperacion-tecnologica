using TPF.RecuperacionTecnologica.Web.ViewModels.Home;

namespace TPF.RecuperacionTecnologica.Web.Services.Home;

public interface IInicioService
{
    Task<InicioUsuarioViewModel?> ObtenerInicioUsuarioAsync(string identityUserId);
}
