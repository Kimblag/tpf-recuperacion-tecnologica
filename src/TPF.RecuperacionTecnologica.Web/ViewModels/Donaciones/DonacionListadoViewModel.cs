using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

public class DonacionListadoViewModel
{
    private string? _busqueda;
    private string? _donante;

    public string? Busqueda
    {
        get => _busqueda;
        set => _busqueda = value is { Length: > 60 } ? value[..60] : value;
    }

    public string? Donante
    {
        get => _donante;
        set => _donante = value is { Length: > 60 } ? value[..60] : value;
    }

    public EstadoEquipo? Estado { get; set; }

    public IReadOnlyList<DonacionGestionFilaViewModel> Filas { get; set; } = [];

    public int Total { get; set; }

    public const int TamanoPagina = 10;

    public int Pagina { get; set; } = 1;

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamanoPagina));

    public int Desde => Total == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;

    public int Hasta => Desde == 0 ? 0 : Desde + Filas.Count - 1;

    public bool ErrorAlCargar { get; set; }

    public bool HayFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda)
        || !string.IsNullOrWhiteSpace(Donante)
        || Estado is not null;
}
