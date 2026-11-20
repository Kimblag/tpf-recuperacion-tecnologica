namespace TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

public class UsuarioDetalleViewModel : UsuarioFilaViewModel
{
    public string? Direccion { get; set; }

    public DateTime? FechaBaja { get; set; }

    public string? MotivoBaja { get; set; }

    public int SolicitudesPendientes { get; set; }

    public int SolicitudesAsignadas { get; set; }

    public int DonacionesPendientes { get; set; }

    public int EquiposDonadosEnCurso { get; set; }

    public IReadOnlyList<TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes.SolicitudFilaViewModel> Solicitudes { get; set; } = [];

    public IReadOnlyList<TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones.DonacionGestionFilaViewModel> Donaciones { get; set; } = [];
}