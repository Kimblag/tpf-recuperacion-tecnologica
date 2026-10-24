using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Entregas;

public enum SituacionEntrega
{
    ElegirAsignacion,
    NoEncontrada,
    NoVigente,
    PlazoVencido,
    Lista
}

public class AsignacionVigenteFilaViewModel
{
    public int NroAsignacion { get; set; }

    public int NroSolicitud { get; set; }

    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string MarcaYModelo { get; set; } = string.Empty;

    public string Solicitante { get; set; } = string.Empty;

    public DateTime FechaLimiteRetiro { get; set; }

    public bool PlazoVencido { get; set; }

    public string Codigo => $"ASG-{NroAsignacion:D6}";

    public string CodigoEquipo => NroEquipo.ToString("D6");
}

public class RegistroEntregaViewModel
{
    public SituacionEntrega Situacion { get; set; }

    public int? NroAsignacion { get; set; }

    [Display(Name = "Nombre y apellido de quien retira")]
    [Required(ErrorMessage = "El nombre y apellido de quien retira es obligatorio.")]
    [StringLength(100, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string NombreQuienRetira { get; set; } = string.Empty;

    [Display(Name = "Documento de quien retira")]
    [Required(ErrorMessage = "El documento de quien retira es obligatorio.")]
    [RegularExpression(@"^\d{7,11}$", ErrorMessage = "El documento debe tener entre 7 y 11 dígitos, sin puntos ni espacios.")]
    public string DocumentoQuienRetira { get; set; } = string.Empty;

    [Display(Name = "Buscar asignación")]
    [StringLength(60, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Busqueda { get; set; }

    public IReadOnlyList<AsignacionVigenteFilaViewModel> Candidatas { get; set; } = [];

    public EstadoAsignacion? EstadoAsignacion { get; set; }

    public int NroSolicitud { get; set; }

    public string Solicitante { get; set; } = string.Empty;

    public TipoSolicitante TipoSolicitante { get; set; }

    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string MarcaYModelo { get; set; } = string.Empty;

    public DateTime FechaLimiteRetiro { get; set; }

    public string Coordinador { get; set; } = string.Empty;

    public DateTime FechaEntrega { get; set; }

    public bool RequiereCoincidenciaDeDocumento => TipoSolicitante == TipoSolicitante.Particular;

    public string CodigoAsignacion => NroAsignacion is null ? string.Empty : $"ASG-{NroAsignacion:D6}";

    public string CodigoSolicitud => $"SOL-{NroSolicitud:D6}";

    public string CodigoEquipo => NroEquipo.ToString("D6");
}
