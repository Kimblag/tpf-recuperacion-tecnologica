using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Asignaciones;

public class AsignacionFilaViewModel
{
    public int NroAsignacion { get; set; }

    public int NroSolicitud { get; set; }

    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string MarcaYModelo { get; set; } = string.Empty;

    public string Coordinador { get; set; } = string.Empty;

    public DateTime FechaAsignacion { get; set; }

    public DateTime FechaLimiteRetiro { get; set; }

    public EstadoAsignacion EstadoAsignacion { get; set; }

    public string Solicitante { get; set; } = string.Empty;

    public bool PlazoVencido { get; set; }

    public bool PlazoPorVencer { get; set; }

    public string Codigo => $"ASG-{NroAsignacion:D6}";

    public string CodigoSolicitud => $"SOL-{NroSolicitud:D6}";

    public string CodigoEquipo => NroEquipo.ToString("D6");
}
