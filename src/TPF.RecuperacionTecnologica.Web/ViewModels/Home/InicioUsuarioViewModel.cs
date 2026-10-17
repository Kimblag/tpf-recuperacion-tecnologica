using TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Home;

public class InicioUsuarioViewModel
{
    public int DonacionesActivas { get; set; }

    public int DonacionesPendientesDeRecepcion { get; set; }

    public int SolicitudesPendientes { get; set; }

    public int EquiposDisponibles { get; set; }

    public int EntregasRecibidas { get; set; }

    public IReadOnlyList<DonacionFilaViewModel> DonacionesRecientes { get; set; } = [];

    public IReadOnlyList<SolicitudFilaViewModel> SolicitudesRecientes { get; set; } = [];

    public IReadOnlyList<ActividadViewModel> Actividad { get; set; } = [];
}