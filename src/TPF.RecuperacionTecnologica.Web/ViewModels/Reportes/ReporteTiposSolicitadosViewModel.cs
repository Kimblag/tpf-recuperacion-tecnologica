using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

public class ReporteTiposSolicitadosViewModel : ReporteBaseViewModel
{
    public IReadOnlyList<TipoEquipoSolicitudesViewModel> Filas { get; set; } = [];

    public int Total => Filas.Sum(f => f.Solicitudes);
}

public class TipoEquipoSolicitudesViewModel
{
    public TipoEquipo TipoEquipo { get; set; }

    public int Solicitudes { get; set; }
}
