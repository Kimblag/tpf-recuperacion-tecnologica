using Microsoft.AspNetCore.Identity;
using TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

namespace TPF.RecuperacionTecnologica.Web.Services.Solicitudes;

public interface ISolicitudService
{
    Task<ImpedimentoSolicitud?> ObtenerImpedimentoAsync(string identityUserId, int nroEquipo);

    Task<ResultadoRegistroSolicitud> RegistrarAsync(string identityUserId, FormularioSolicitudViewModel modelo);

    Task<IdentityResult> EditarMotivoAsync(string identityUserId, int nroSolicitud, string motivo);

    Task<IdentityResult> CancelarAsync(string identityUserId, int nroSolicitud);
}