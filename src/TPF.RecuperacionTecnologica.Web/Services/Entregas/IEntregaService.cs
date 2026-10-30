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
}
