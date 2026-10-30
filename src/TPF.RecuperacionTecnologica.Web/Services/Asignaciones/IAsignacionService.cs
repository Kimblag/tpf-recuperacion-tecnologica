using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Asignaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.Services.Asignaciones;

public interface IAsignacionService
{
    /// <summary>
    /// Solicitudes pendientes que piden el equipo, en orden de prioridad: la primera es la sugerida.
    /// </summary>
    Task<IReadOnlyList<SolicitudCandidataViewModel>> ObtenerCandidatasAsync(int nroEquipo);

    /// <summary>
    /// Equipos Disponibles que tienen al menos una solicitud pendiente, los más recientes primero.
    /// </summary>
    Task<IReadOnlyList<EquipoSeleccionableViewModel>> ObtenerEquiposConSolicitudPendienteAsync(string? busqueda, int cantidad);

    Task<ResultadoPaginado<AsignacionFilaViewModel>> ListarAsync(
        string? busqueda, EstadoAsignacion? estado, FiltroPlazoAsignacion? plazo, int pagina, int tamanoPagina);

    Task<AsignacionDetalleViewModel?> ObtenerDetalleAsync(int nroAsignacion);

    Task<ResultadoAsignacion> LiberarAsync(string identityUserIdCoordinador, int nroAsignacion, string? motivoLiberacion);

    Task<ResultadoAsignacion> AsignarAsync(
        string identityUserIdCoordinador, int nroEquipo, int nroSolicitud, string? justificacionCambio);
}
