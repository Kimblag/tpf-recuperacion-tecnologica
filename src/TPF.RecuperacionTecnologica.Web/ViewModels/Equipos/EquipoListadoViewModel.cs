using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

public class EquipoListadoViewModel
{
    private string? _busqueda;

    public string? Busqueda
    {
        get => _busqueda;
        set => _busqueda = value is { Length: > 60 } ? value[..60] : value;
    }

    public TipoEquipo? Tipo { get; set; }

    public EstadoEquipo? Estado { get; set; }

    public IReadOnlyList<EquipoFilaViewModel> Filas { get; set; } = [];

    public int Total { get; set; }

    public bool ErrorAlCargar { get; set; }

    public bool HayFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda)
        || Tipo is not null
        || Estado is not null;
}
