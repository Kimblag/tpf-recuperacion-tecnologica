using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Entregas;

namespace TPF.RecuperacionTecnologica.Web.Services.Entregas;

public interface IEntregaService
{
    /// <summary>
    /// Asignaciones Vigentes que coinciden con la búsqueda, las que vencen antes primero.
    /// </summary>
    Task<IReadOnlyList<AsignacionVigenteFilaViewModel>> BuscarAsignacionesVigentesAsync(string? busqueda, int cantidad);

    /// <summary>
    /// Datos de la asignación y si se puede entregar; null si no existe.
    /// </summary>
    Task<RegistroEntregaViewModel?> ObtenerParaRegistrarAsync(int nroAsignacion);

    Task<ResultadoEntrega> RegistrarAsync(
        string identityUserIdCoordinador, int nroAsignacion, string? nombreQuienRetira, string? documentoQuienRetira);

    /// <summary>
    /// Historial de entregas, de la más reciente a la más antigua. La búsqueda y las fechas son opcionales;
    /// <paramref name="hasta"/> incluye todo ese día.
    /// </summary>
    /// <exception cref="ArgumentException">Si <paramref name="desde"/> es posterior a <paramref name="hasta"/>.</exception>
    Task<ResultadoPaginado<EntregaFilaViewModel>> ListarAsync(
        string? busqueda, DateTime? desde, DateTime? hasta, int pagina, int tamanoPagina);

    /// <summary>
    /// Detalle de solo lectura de una entrega; null si no existe.
    /// </summary>
    Task<EntregaDetalleViewModel?> ObtenerDetalleAsync(int nroEntrega);
}
