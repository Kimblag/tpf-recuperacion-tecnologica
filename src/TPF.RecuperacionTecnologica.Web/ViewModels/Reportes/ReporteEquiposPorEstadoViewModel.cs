using TPF.RecuperacionTecnologica.Web.ViewModels.Administrador;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

public class ReporteEquiposPorEstadoViewModel : ReporteBaseViewModel
{
    public IReadOnlyList<EquipoEstadoCantidadViewModel> Estados { get; set; } = [];

    public int Total => Estados.Sum(e => e.Cantidad);
}
