using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;

public class CatalogoListadoViewModel
{

    public string? _busqueda;

    public string? Busqueda 
    {
        get => _busqueda;
        set => _busqueda = value is { Length: > 60 } ? value[..60] : value;
    }
    public string? Marca { get; set; }
    public TipoEquipo? Tipo { get; set; }
    public OrdenCatalogo Orden { get; set; } = OrdenCatalogo.Recientes;
    public IReadOnlyList<EquipoDisponibleViewModel> Filas { get; set; } = [];
    public IReadOnlyList<string> Marcas { get; set; } = [];
    public int Total { get; set; }

    public int TotalDisponibles { get; set; }

    public const int TamanoPagina = 12;

    public int Pagina { get; set; } = 1;

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamanoPagina));

    public int Desde => Total == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;

    public int Hasta => Desde == 0 ? 0 : Desde + Filas.Count - 1;

    public bool ErrorAlCargar { get; set; }

    public bool HayFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda)
        || !string.IsNullOrWhiteSpace(Marca)
        || Tipo is not null;
}
