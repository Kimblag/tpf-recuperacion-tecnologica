using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Asignaciones;

public class AsignacionDetalleViewModel
{
    public int NroAsignacion { get; set; }

    public int NroSolicitud { get; set; }

    public string Solicitante { get; set; } = string.Empty;

    public TipoSolicitante TipoSolicitante { get; set; }

    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string MarcaYModelo { get; set; } = string.Empty;

    public string Coordinador { get; set; } = string.Empty;

    public DateTime FechaAsignacion { get; set; }

    public DateTime FechaLimiteRetiro { get; set; }

    public EstadoAsignacion EstadoAsignacion { get; set; }

    public bool CambioPrioridad { get; set; }

    public string? JustificacionCambio { get; set; }

    public bool PlazoVencido { get; set; }

    public DateTime? FechaLiberacion { get; set; }

    public string? MotivoLiberacion { get; set; }

    public IReadOnlyList<HitoViewModel> Hitos { get; set; } = [];

    public LiberarAsignacionViewModel Liberacion { get; set; } = new();

    public string Codigo => $"ASG-{NroAsignacion:D6}";

    public string CodigoSolicitud => $"SOL-{NroSolicitud:D6}";

    public string CodigoEquipo => NroEquipo.ToString("D6");

    public bool EsVigente => EstadoAsignacion == EstadoAsignacion.Vigente;
}
