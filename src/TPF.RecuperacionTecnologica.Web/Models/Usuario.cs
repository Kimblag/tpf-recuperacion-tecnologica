using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.Models;

public class Usuario
{
    public int NroUsuario { get; set; }

    public string IdentityUserId { get; set; } = string.Empty;

    public TipoUsuario TipoUsuario { get; set; }

    public string? Nombre { get; set; }

    public string? Apellido { get; set; }

    public string? RazonSocial { get; set; }

    public TipoDocumento TipoDocumento { get; set; }

    public string NroDocumento { get; set; } = string.Empty;

    public string? Direccion { get; set; }

    public string? Telefono { get; set; }

    public string CorreoElectronico { get; set; } = string.Empty;

    public EstadoUsuario EstadoUsuario { get; set; }

    public DateTime FechaAlta { get; set; }

    public DateTime? FechaUltimaModificacion { get; set; }

    public DateTime? FechaBaja { get; set; }

    public string? MotivoBaja { get; set; }
}
