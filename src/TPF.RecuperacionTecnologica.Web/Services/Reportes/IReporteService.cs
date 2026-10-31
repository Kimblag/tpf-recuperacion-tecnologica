using TPF.RecuperacionTecnologica.Web.ViewModels.Administrador;
using TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

namespace TPF.RecuperacionTecnologica.Web.Services.Reportes;

/// <summary>
/// Informes de gestión del Administrador. En todos, <c>desde</c> y <c>hasta</c> son opcionales:
/// sin fechas se incluye todo lo disponible (acceso rápido) y <c>hasta</c> incluye todo ese día.
/// </summary>
public interface IReporteService
{
    /// <summary>
    /// Equipos registrados en el rango, agrupados por su estado actual.
    /// </summary>
    /// <exception cref="ArgumentException">Si <paramref name="desde"/> es posterior a <paramref name="hasta"/>.</exception>
    Task<IReadOnlyList<EquipoEstadoCantidadViewModel>> EquiposPorEstadoAsync(DateTime? desde, DateTime? hasta);

    /// <summary>
    /// Entregas del rango agrupadas por mes, trimestre o año, incluidos los períodos sin entregas.
    /// </summary>
    /// <exception cref="ArgumentException">Si <paramref name="desde"/> es posterior a <paramref name="hasta"/>.</exception>
    Task<IReadOnlyList<PeriodoEntregasViewModel>> EntregasPorPeriodoAsync(
        DateTime? desde, DateTime? hasta, AgrupacionPeriodo agrupacion);

    /// <summary>
    /// Solicitudes del rango agrupadas por el tipo del equipo pedido, de la más pedida a la menos pedida.
    /// </summary>
    /// <exception cref="ArgumentException">Si <paramref name="desde"/> es posterior a <paramref name="hasta"/>.</exception>
    Task<IReadOnlyList<TipoEquipoSolicitudesViewModel>> TiposSolicitadosAsync(DateTime? desde, DateTime? hasta);
}
