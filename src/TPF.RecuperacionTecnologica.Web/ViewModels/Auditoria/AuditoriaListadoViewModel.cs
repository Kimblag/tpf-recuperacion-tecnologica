using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Auditoria;

public class AuditoriaListadoViewModel : IValidatableObject
{
    // clave: el nombre de entidad que guardan los servicios (nameof); valor: la etiqueta que se muestra
    public static readonly IReadOnlyDictionary<string, string> EntidadesDisponibles = new Dictionary<string, string>
    {
        ["Equipo"] = "Equipo",
        ["Solicitud"] = "Solicitud",
        ["Asignacion"] = "Asignación",
        ["Entrega"] = "Entrega",
        ["Diagnostico"] = "Diagnóstico",
        ["Usuario"] = "Usuario",
        ["Personal"] = "Personal",
        ["ConfiguracionInstitucional"] = "Parámetros"
    };

    public static string EtiquetaEntidad(string entidad) =>
        EntidadesDisponibles.TryGetValue(entidad, out var etiqueta) ? etiqueta : entidad;

    [Display(Name = "Desde")]
    [DataType(DataType.Date)]
    public DateTime? FechaDesde { get; set; }

    [Display(Name = "Hasta")]
    [DataType(DataType.Date)]
    public DateTime? FechaHasta { get; set; }

    public TipoOperacionAuditoria? Operacion { get; set; }

    [Display(Name = "Código de personal")]
    [StringLength(20)]
    public string? CodigoPersonal { get; set; }

    public string? Entidad { get; set; }

    [Display(Name = "Identificador")]
    [StringLength(20)]
    public string? Identificador { get; set; }

    // el código de personal se muestra como PER-0002 y el identificador como SOL-000030; también se acepta solo el número
    public int? NumeroCodigoPersonal => LeerNumero(CodigoPersonal);

    public int? NumeroIdentificador => LeerNumero(Identificador);

    public IReadOnlyList<AuditoriaFilaViewModel> Filas { get; set; } = [];

    public int Total { get; set; }

    public const int TamanoPagina = 10;

    public int Pagina { get; set; } = 1;

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamanoPagina));

    public int Desde => Total == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;

    public int Hasta => Desde == 0 ? 0 : Desde + Filas.Count - 1;

    public bool ErrorAlCargar { get; set; }

    public bool HayFiltros =>
        FechaDesde is not null
        || FechaHasta is not null
        || Operacion is not null
        || !string.IsNullOrWhiteSpace(CodigoPersonal)
        || !string.IsNullOrWhiteSpace(Entidad)
        || !string.IsNullOrWhiteSpace(Identificador);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaDesde is not null && FechaHasta is not null && FechaDesde.Value.Date > FechaHasta.Value.Date)
        {
            yield return new ValidationResult(
                "La fecha desde no puede ser posterior a la fecha hasta.", [nameof(FechaDesde)]);
        }

        if (!string.IsNullOrWhiteSpace(CodigoPersonal) && NumeroCodigoPersonal is null)
        {
            yield return new ValidationResult(
                "El código de personal debe ser un número o un código como PER-0002.", [nameof(CodigoPersonal)]);
        }

        if (!string.IsNullOrWhiteSpace(Identificador) && NumeroIdentificador is null)
        {
            yield return new ValidationResult(
                "El identificador debe ser un número o un código como SOL-000030.", [nameof(Identificador)]);
        }
    }

    private static int? LeerNumero(string? texto)
    {
        var termino = texto?.Trim();

        if (string.IsNullOrEmpty(termino))
        {
            return null;
        }

        var guion = termino.IndexOf('-');

        if (guion >= 0)
        {
            termino = termino[(guion + 1)..];
        }

        return int.TryParse(termino, out var numero) && numero > 0 ? numero : null;
    }
}
