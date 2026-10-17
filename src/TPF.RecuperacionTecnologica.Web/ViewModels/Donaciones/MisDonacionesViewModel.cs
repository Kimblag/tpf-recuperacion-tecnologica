using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

public class MisDonacionesViewModel
{
    private string? _busqueda;

    public string? Busqueda
    {
        get => _busqueda;
        set => _busqueda = value is { Length: > 60 } ? value[..60] : value;
    }

    public EstadoEquipo? Estado { get; set; }

    public TipoEquipo? Tipo { get; set; }

    public EstadoInicialDeclarado? Condicion { get; set; }

    public IReadOnlyList<DonacionFilaViewModel> Filas { get; set; } = [];

    public int Total { get; set; }

    public bool ErrorAlCargar { get; set; }

    public bool HayFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda)
        || Estado is not null
        || Tipo is not null
        || Condicion is not null;
}