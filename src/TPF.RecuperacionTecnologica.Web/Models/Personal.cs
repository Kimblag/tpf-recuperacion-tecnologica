using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.Models;

public class Personal
{
    public int CodigoPersonal { get; set; }

    public string IdentityUserId { get; set; } = string.Empty;

    public string Dni { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Apellido { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    public string CorreoElectronico { get; set; } = string.Empty;

    public RolPersonal RolAsignado { get; set; }

    public EstadoPersonal EstadoPersonal { get; set; }

    public DateTime FechaAlta { get; set; }

    public DateTime? FechaUltimaModificacion { get; set; }

    public DateTime? FechaInactivacion { get; set; }

    public string? MotivoInactivacion { get; set; }
}
