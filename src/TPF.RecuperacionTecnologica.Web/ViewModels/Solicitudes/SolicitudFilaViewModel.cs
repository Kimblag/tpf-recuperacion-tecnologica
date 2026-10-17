using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

public class SolicitudFilaViewModel
{
    public int NroSolicitud { get; set; }

    public int NroEquipo { get; set; }

    public TipoSolicitante TipoSolicitante { get; set; }

    public OrigenSolicitud OrigenSolicitud { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public EstadoSolicitud EstadoSolicitud { get; set; }

    public string Codigo => $"SOL-{NroSolicitud:D6}";

    public string CodigoEquipo => NroEquipo.ToString("D6");
}