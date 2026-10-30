using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Entregas;

public class EntregaListadoViewModel : IValidatableObject
{
    private string? _busqueda;

    public string? Busqueda
    {
        get => _busqueda;
        set => _busqueda = value is { Length: > 60 } ? value[..60] : value;
    }

    [Display(Name = "Desde")]
    public DateTime? FechaDesde { get; set; }

    [Display(Name = "Hasta")]
    public DateTime? FechaHasta { get; set; }

    public IReadOnlyList<EntregaFilaViewModel> Filas { get; set; } = [];

    public int Total { get; set; }

    public const int TamanoPagina = 10;

    public int Pagina { get; set; } = 1;

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamanoPagina));

    public int Desde => Total == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;

    public int Hasta => Desde == 0 ? 0 : Desde + Filas.Count - 1;

    public bool ErrorAlCargar { get; set; }

    public bool HayFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda)
        || FechaDesde is not null
        || FechaHasta is not null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaDesde is not null && FechaHasta is not null && FechaDesde.Value.Date > FechaHasta.Value.Date)
        {
            yield return new ValidationResult(
                "La fecha desde no puede ser posterior a la fecha hasta.", [nameof(FechaDesde)]);
        }
    }
}
