using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Home;

public class SolicitudPrioritariaViewModel
{
    public int NroSolicitud { get; set; }

    public string Solicitante { get; set; } = string.Empty;

    public TipoSolicitante TipoSolicitante { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public int NroEquipo { get; set; }

    public string MarcaYModelo { get; set; } = string.Empty;

    public DateTime FechaSolicitud { get; set; }

    public string Codigo => $"SOL-{NroSolicitud:D6}";

    public string CodigoEquipo => NroEquipo.ToString("D6");
}

public class AsignacionConPlazoViewModel
{
    public int NroAsignacion { get; set; }

    public string Solicitante { get; set; } = string.Empty;

    public string MarcaYModelo { get; set; } = string.Empty;

    public DateTime FechaLimiteRetiro { get; set; }

    public int DiasRestantes { get; set; }

    public bool PlazoVencido => DiasRestantes < 0;

    public string Codigo => $"ASG-{NroAsignacion:D6}";
}

public class CoordinadorInicioViewModel
{
    public int EquiposDisponibles { get; set; }

    public int DisponiblesEstaSemana { get; set; }

    public int SolicitudesPendientes { get; set; }

    public int PendientesInstitucionales { get; set; }

    public int AsignacionesVigentes { get; set; }

    public int AsignacionesPorVencer { get; set; }

    public int AsignacionesVencidas { get; set; }

    public int EntregasDelMes { get; set; }

    public int EntregasDelMesAnterior { get; set; }

    public IReadOnlyList<SolicitudPrioritariaViewModel> ColaPrioritaria { get; set; } = [];

    public IReadOnlyList<AsignacionConPlazoViewModel> AsignacionesConPlazo { get; set; } = [];

    public IReadOnlyList<ActividadViewModel> ActividadReciente { get; set; } = [];

    public bool ErrorAlCargar { get; set; }
}
