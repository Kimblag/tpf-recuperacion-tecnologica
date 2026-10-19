using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Home;

public class TecnicoInicioViewModel
{
    public int PendientesDeDiagnostico { get; set; }

    public DateTime? FechaPendienteMasAntiguo { get; set; }

    public int EnReparacion { get; set; }

    public int DiagnosticosDelMes { get; set; }

    public int EquiposDisponibles { get; set; }

    public IReadOnlyList<EquipoFilaViewModel> Pendientes { get; set; } = [];

    public IReadOnlyList<ActividadViewModel> ActividadReciente { get; set; } = [];

    public bool ErrorAlCargar { get; set; }
}
