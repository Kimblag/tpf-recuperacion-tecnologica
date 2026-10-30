using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

public class AsignacionLegajoViewModel
{
    public int NroAsignacion { get; set; }

    public int NroSolicitud { get; set; }

    public string Coordinador { get; set; } = string.Empty;

    public DateTime FechaAsignacion { get; set; }

    public DateTime FechaLimiteRetiro { get; set; }

    public EstadoAsignacion EstadoAsignacion { get; set; }

    public DateTime? FechaLiberacion { get; set; }

    public string? MotivoLiberacion { get; set; }

    public string Codigo => $"ASG-{NroAsignacion:D6}";

    public string CodigoSolicitud => $"SOL-{NroSolicitud:D6}";
}
