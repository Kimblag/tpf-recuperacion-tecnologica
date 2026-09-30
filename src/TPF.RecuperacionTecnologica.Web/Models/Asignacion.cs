using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.Models;

public class Asignacion
{
    public int NroAsignacion { get; set; }

    public int NroEquipo { get; set; }

    public Equipo Equipo { get; set; } = null!;

    public int NroSolicitud { get; set; }

    public Solicitud Solicitud { get; set; } = null!;

    public int CodigoCoordinador { get; set; }

    public Personal Coordinador { get; set; } = null!;

    public DateTime FechaAsignacion { get; set; }

    public DateTime? FechaUltimaModificacion { get; set; }

    public string? JustificacionCambio { get; set; }

    public EstadoAsignacion EstadoAsignacion { get; set; }

    public DateTime? FechaLiberacion { get; set; }

    public string? MotivoLiberacion { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
