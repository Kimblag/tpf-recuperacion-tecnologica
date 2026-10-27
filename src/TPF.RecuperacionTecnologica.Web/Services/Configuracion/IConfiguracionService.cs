using Microsoft.AspNetCore.Identity;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.ViewModels.Parametros;

namespace TPF.RecuperacionTecnologica.Web.Services.Configuracion;

public interface IConfiguracionService
{
    Task<ConfiguracionInstitucional?> ObtenerAsync();

    Task<IdentityResult> EditarAsync(string identityUserIdAdministrador, ParametrosViewModel modelo);
}
