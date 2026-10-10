using System.Globalization;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Personal;

public class PersonalFilaViewModel
{
    public int CodigoPersonal { get; set; }

    public string Dni { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Apellido { get; set; } = string.Empty;

    public string CorreoElectronico { get; set; } = string.Empty;

    public RolPersonal RolAsignado { get; set; }

    public EstadoPersonal EstadoPersonal { get; set; }

    public string Codigo => $"PER-{CodigoPersonal:D4}";

    public string NombreCompleto => $"{Nombre} {Apellido}";

    // en pantalla se muestra con puntos = 27.884.310
    public string DniFormateado =>
        long.TryParse(Dni, out var numero) ? numero.ToString("N0", CultureInfo.GetCultureInfo("es-AR")) : Dni;
}
