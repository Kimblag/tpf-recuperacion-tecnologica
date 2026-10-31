using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;

public class EquipoCatalogoDetalleViewModel : EquipoDisponibleViewModel
{
    public DictamenTecnico DictamenTecnico { get; set; }

    public DateTime FechaRevision { get; set; }

    public IReadOnlyList<FotoEquipoViewModel> Imagenes { get; set; } = [];
}