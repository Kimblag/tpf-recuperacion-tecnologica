using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Entregas;

public class EntregaDetalleViewModel : EntregaFilaViewModel
{
    public TipoSolicitante TipoSolicitante { get; set; }

    public DateTime FechaAsignacion { get; set; }

    public DateTime FechaLimiteRetiro { get; set; }

    public IReadOnlyList<HitoViewModel> Hitos { get; set; } = [];
}
