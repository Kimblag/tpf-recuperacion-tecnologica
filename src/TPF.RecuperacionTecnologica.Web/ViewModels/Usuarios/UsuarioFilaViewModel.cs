using System.Globalization;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

public class UsuarioFilaViewModel
{
    public int NroUsuario { get; set; }

    public TipoUsuario TipoUsuario { get; set; }

    public string? Nombre { get; set; }

    public string? Apellido { get; set; }

    public string? RazonSocial { get; set; }

    public TipoDocumento TipoDocumento { get; set; }

    public string NroDocumento { get; set; } = string.Empty;

    public string CorreoElectronico { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    public DateTime FechaAlta { get; set; }

    public EstadoUsuario EstadoUsuario { get; set; }

    public string Codigo => $"U-{NroUsuario:D4}";

    public string NombreVisible =>
        TipoUsuario == TipoUsuario.Persona ? $"{Nombre} {Apellido}".Trim() : RazonSocial ?? string.Empty;

    public string DocumentoFormateado
    {
        get
        {
            if (TipoDocumento == TipoDocumento.Cuit && NroDocumento.Length == 11)
            {
                return $"{NroDocumento[..2]}-{NroDocumento[2..10]}-{NroDocumento[10]}";
            }

            return long.TryParse(NroDocumento, out var numero)
                ? numero.ToString("N0", CultureInfo.GetCultureInfo("es-AR"))
                : NroDocumento;
        }
    }
}
