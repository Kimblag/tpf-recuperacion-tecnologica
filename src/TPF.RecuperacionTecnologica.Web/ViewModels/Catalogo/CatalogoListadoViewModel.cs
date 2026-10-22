using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;

public class CatalogoListadoViewModel
{
    public TipoEquipo? Tipo { get; set; }

    public IReadOnlyList<EquipoDisponibleViewModel> Filas { get; set; } = [];

    public bool ErrorAlCargar { get; set; }

    public bool HayFiltros => Tipo is not null;
}
