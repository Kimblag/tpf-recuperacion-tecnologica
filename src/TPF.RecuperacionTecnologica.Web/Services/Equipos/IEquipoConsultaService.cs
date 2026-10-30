using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.Services.Equipos;

public interface IEquipoConsultaService
{
    /// <summary>
    /// Lista los equipos para el personal interno, con filtros opcionales de búsqueda, tipo y estado.
    /// Con soloPendientesDeDiagnostico limita el listado a los estados que esperan una acción de diagnóstico
    /// (Pendiente de recepción y En reparación).
    /// </summary>
    /// <returns>
    /// La página pedida, ajustada al rango válido, o vacía si ningún equipo cumple los filtros.
    /// </returns>
    Task<ResultadoPaginado<EquipoFilaViewModel>> ListarAsync(
        string? busqueda, EstadoEquipo? estado, TipoEquipo? tipo, bool soloPendientesDeDiagnostico,
        OrdenEquipo orden, int pagina, int tamanoPagina);

    /// <summary>
    /// Devuelve los datos de un equipo, de su donante y de su diagnóstico vigente para el personal interno.
    /// </summary>
    /// <returns>Los datos del equipo, o null si no existe.</returns>
    Task<EquipoDetalleTecnicoViewModel?> ObtenerDetalleAsync(int nroEquipo);

    /// <summary>
    /// Devuelve el historial completo de diagnósticos de un equipo, del más reciente al más antiguo.
    /// </summary>
    /// <returns>El historial, o null si el equipo no existe.</returns>
    Task<HistorialDiagnosticosViewModel?> ObtenerHistorialAsync(int nroEquipo);

    /// <summary>
    /// Devuelve el legajo completo de un equipo para el Coordinador y el Administrador: sus datos, el donante
    /// (sin documento), todos sus diagnósticos, solicitudes y asignaciones en orden cronológico, y su entrega si existe.
    /// </summary>
    /// <returns>El legajo, o null si el equipo no existe.</returns>
    Task<LegajoEquipoViewModel?> ObtenerLegajoAsync(int nroEquipo);
}
