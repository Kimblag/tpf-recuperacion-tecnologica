using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

namespace TPF.RecuperacionTecnologica.Web.Services.Solicitudes;

public interface ISolicitudConsultaService
{
    Task<ResultadoPaginado<SolicitudFilaViewModel>> ListarPropiasAsync(
        string identityUserId, string? busqueda, EstadoSolicitud? estado, int pagina, int tamanoPagina);

    Task<SolicitudDetalleViewModel?> ObtenerDetallePropioAsync(string identityUserId, int nroSolicitud);
}