using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Entregas;

public class EntregaFilaViewModel
{
    public int NroEntrega { get; set; }

    public int NroAsignacion { get; set; }

    public int NroSolicitud { get; set; }

    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string MarcaYModelo { get; set; } = string.Empty;

    public string Solicitante { get; set; } = string.Empty;

    public string Coordinador { get; set; } = string.Empty;

    public DateTime FechaEntrega { get; set; }

    public string NombreQuienRetira { get; set; } = string.Empty;

    public string DocumentoQuienRetira { get; set; } = string.Empty;

    public string Codigo => $"ENT-{NroEntrega:D6}";

    public string CodigoAsignacion => $"ASG-{NroAsignacion:D6}";

    public string CodigoSolicitud => $"SOL-{NroSolicitud:D6}";

    public string CodigoEquipo => NroEquipo.ToString("D6");
}
