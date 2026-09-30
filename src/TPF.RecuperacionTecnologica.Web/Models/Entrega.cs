namespace TPF.RecuperacionTecnologica.Web.Models;

public class Entrega
{
    public int NroEntrega { get; set; }

    public int NroEquipo { get; set; }

    public Equipo Equipo { get; set; } = null!;

    public int NroSolicitud { get; set; }

    public Solicitud Solicitud { get; set; } = null!;

    public int NroAsignacion { get; set; }

    public Asignacion Asignacion { get; set; } = null!;

    public int CodigoCoordinador { get; set; }

    public Personal Coordinador { get; set; } = null!;

    public DateTime FechaEntrega { get; set; }

    public string DocumentoIdentidadQuienRetira { get; set; } = string.Empty;
}
