using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

public class SolicitudLegajoViewModel
{
    public int NroSolicitud { get; set; }

    public string Solicitante { get; set; } = string.Empty;

    public TipoSolicitante TipoSolicitante { get; set; }

    public OrigenSolicitud OrigenSolicitud { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public EstadoSolicitud EstadoSolicitud { get; set; }

    public DateTime? FechaCancelacion { get; set; }

    public string Codigo => $"SOL-{NroSolicitud:D6}";
}
