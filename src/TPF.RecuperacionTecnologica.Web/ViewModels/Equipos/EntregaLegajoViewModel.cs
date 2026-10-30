namespace TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

// no incluye el documento de quien retiró el equipo
public class EntregaLegajoViewModel
{
    public int NroEntrega { get; set; }

    public int NroAsignacion { get; set; }

    public int NroSolicitud { get; set; }

    public string Coordinador { get; set; } = string.Empty;

    public DateTime FechaEntrega { get; set; }

    public string Codigo => $"ENT-{NroEntrega:D6}";

    public string CodigoAsignacion => $"ASG-{NroAsignacion:D6}";
}
