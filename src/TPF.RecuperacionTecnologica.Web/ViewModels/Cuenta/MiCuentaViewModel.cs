using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;

public class MiCuentaViewModel
{
    public int CodigoPersonal { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Apellido { get; set; } = string.Empty;

    public string CorreoElectronico { get; set; } = string.Empty;

    public RolPersonal RolAsignado { get; set; }

    public EstadoPersonal EstadoPersonal { get; set; }

    public string Codigo => $"PER-{CodigoPersonal:D4}";

    public string NombreCompleto => $"{Nombre} {Apellido}";
}
