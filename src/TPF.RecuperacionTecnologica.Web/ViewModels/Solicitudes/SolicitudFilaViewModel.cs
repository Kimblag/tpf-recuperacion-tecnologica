using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

public class SolicitudFilaViewModel
{
    public int NroSolicitud { get; set; }

    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string MarcaYModelo { get; set; } = string.Empty;

    public TipoSolicitante TipoSolicitante { get; set; }

    public OrigenSolicitud OrigenSolicitud { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public EstadoSolicitud EstadoSolicitud { get; set; }

    public string Codigo => $"SOL-{NroSolicitud:D6}";

    public string CodigoEquipo => NroEquipo.ToString("D6");
}
