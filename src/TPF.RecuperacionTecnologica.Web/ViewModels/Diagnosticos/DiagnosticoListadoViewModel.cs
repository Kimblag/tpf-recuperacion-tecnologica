using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Diagnosticos;

public class DiagnosticoListadoViewModel
{
    private string? _busqueda;

    public string? Busqueda
    {
        get => _busqueda;
        set => _busqueda = value is { Length: > 60 } ? value[..60] : value;
    }

    public TipoDiagnostico? Tipo { get; set; }

    public DictamenTecnico? Dictamen { get; set; }

    public IReadOnlyList<DiagnosticoFilaViewModel> Filas { get; set; } = [];

    public int Total { get; set; }

    public const int TamanoPagina = 10;

    public int Pagina { get; set; } = 1;

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamanoPagina));

    public int Desde => Total == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;

    public int Hasta => Desde == 0 ? 0 : Desde + Filas.Count - 1;

    public bool ErrorAlCargar { get; set; }

    public bool HayFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda)
        || Tipo is not null
        || Dictamen is not null;
}
