using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.Models;

public class Solicitud
{
    public int NroSolicitud { get; set; }

    public int NroUsuario { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public TipoSolicitante TipoSolicitante { get; set; }

    public int NroEquipoRequerido { get; set; }

    public Equipo EquipoRequerido { get; set; } = null!;

    public string MotivoSolicitud { get; set; } = string.Empty;

    public OrigenSolicitud OrigenSolicitud { get; set; }

    public int? CodigoCoordinador { get; set; }

    public Personal? Coordinador { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public DateTime? FechaUltimaModificacion { get; set; }

    public EstadoSolicitud EstadoSolicitud { get; set; }

    public string? MotivoCancelacion { get; set; }

    public DateTime? FechaCancelacion { get; set; }

    public DateTime? FechaCierre { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
