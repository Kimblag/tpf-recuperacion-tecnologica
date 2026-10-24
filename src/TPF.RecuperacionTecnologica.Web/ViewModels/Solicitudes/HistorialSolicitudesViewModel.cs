using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

public class HistorialSolicitudesViewModel
{
    public IReadOnlyList<HitoViewModel> Movimientos { get; set; } = [];

    public bool ErrorAlCargar { get; set; }
}
