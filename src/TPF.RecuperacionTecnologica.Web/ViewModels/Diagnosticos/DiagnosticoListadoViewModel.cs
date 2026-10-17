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

    public bool ErrorAlCargar { get; set; }

    public bool HayFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda)
        || Tipo is not null
        || Dictamen is not null;
}
