using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

// ficha de solo lectura para el Coordinador: no incluye el documento, que se compara al entregar un equipo
public class FichaUsuarioViewModel
{
    public int NroUsuario { get; set; }

    public TipoUsuario TipoUsuario { get; set; }

    public string? Nombre { get; set; }

    public string? Apellido { get; set; }

    public string? RazonSocial { get; set; }

    public string CorreoElectronico { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    public DateTime FechaAlta { get; set; }

    public EstadoUsuario EstadoUsuario { get; set; }

    public bool TieneContrasena { get; set; }

    public IReadOnlyList<SolicitudFilaViewModel> Solicitudes { get; set; } = [];

    public IReadOnlyList<DonacionGestionFilaViewModel> Donaciones { get; set; } = [];

    public string Codigo => $"U-{NroUsuario:D4}";

    public string NombreVisible =>
        TipoUsuario == TipoUsuario.Persona ? $"{Nombre} {Apellido}".Trim() : RazonSocial ?? string.Empty;
}
