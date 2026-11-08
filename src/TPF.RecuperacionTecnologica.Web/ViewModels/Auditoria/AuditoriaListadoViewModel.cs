using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Reloj;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

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

    public const int LimiteExportacion = 5000;

    public string DescripcionFiltros()
    {
        var filtros = new List<string>();

        if (FechaDesde is not null)
        {
            filtros.Add($"desde {FechaDesde:dd/MM/yyyy}");
        }

        if (FechaHasta is not null)
        {
            filtros.Add($"hasta {FechaHasta:dd/MM/yyyy}");
        }

        if (Operacion is not null)
        {
            filtros.Add($"operación {NombreEnum.De(Operacion.Value)}");
        }

        if (!string.IsNullOrWhiteSpace(CodigoPersonal))
        {
            filtros.Add($"personal {CodigoPersonal.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(Entidad))
        {
            filtros.Add($"entidad {EtiquetaEntidad(Entidad.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(Identificador))
        {
            filtros.Add($"identificador {Identificador.Trim()}");
        }

        return filtros.Count == 0
            ? "Sin filtros: todas las operaciones registradas."
            : $"Filtros: {string.Join(" · ", filtros)}.";
    }

    public TablaExportable ATabla(IRelojInstitucional reloj) => new()
    {
        Titulo = "Registro de auditoría",
        Descripcion = Total > LimiteExportacion
            ? $"Se incluyen las {LimiteExportacion:N0} operaciones más recientes de {Total:N0}. Acotá las fechas para exportar el resto."
            : $"{Total:N0} {(Total == 1 ? "operación registrada" : "operaciones registradas")}.",
        Periodo = DescripcionFiltros(),
        NombreArchivo = "auditoria",
        Horizontal = true,
        Columnas =
        [
            new ColumnaExportable("Fecha y hora", peso: 1.4f),
            new ColumnaExportable("Personal", peso: 2f),
            new ColumnaExportable("Operación", peso: 1.8f),
            new ColumnaExportable("Entidad", peso: 1.2f),
            new ColumnaExportable("Identificador", peso: 1.3f),
            new ColumnaExportable("Detalle", peso: 3.5f)
        ],
        Filas = Filas
            .Select(f => (IReadOnlyList<object>)new object[]
            {
                reloj.FechaHora(f.FechaHora),
                f.Personal,
                NombreEnum.De(f.Operacion),
                f.EtiquetaEntidad,
                f.Identificador,
                f.Detalle ?? string.Empty
            })
            .ToList()
    };

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
