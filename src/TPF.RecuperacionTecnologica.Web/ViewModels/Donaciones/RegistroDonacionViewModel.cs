using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

public class RegistroDonacionViewModel : IValidatableObject
{
    // los enums son nullable para que el select arranque sin opción elegida y [Required] funcione
    [Display(Name = "Tipo de equipo")]
    [Required(ErrorMessage = "El tipo de equipo es obligatorio.")]
    [EnumDataType(typeof(TipoEquipo), ErrorMessage = "Seleccioná un tipo de equipo válido.")]
    public TipoEquipo? TipoEquipo { get; set; }

    [Required(ErrorMessage = "La marca es obligatoria.")]
    [StringLength(60, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [StringLength(60, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string Modelo { get; set; } = string.Empty;

    // número de serie, o IMEI si es un celular; el formato depende del tipo y se valida en Validate
    [Display(Name = "Número de serie")]
    [StringLength(ValidacionesNumeroSerie.LargoMaximo, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? NumeroSerie { get; set; }

    [Display(Name = "Estado del equipo")]
    [Required(ErrorMessage = "El estado del equipo es obligatorio.")]
    [EnumDataType(typeof(EstadoInicialDeclarado), ErrorMessage = "Seleccioná un estado válido.")]
    public EstadoInicialDeclarado? EstadoInicialDeclarado { get; set; }

    [StringLength(500, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Observaciones { get; set; }

    // especificaciones opcionales: arman EspecificacionesDeclaradas del Equipo
    [StringLength(100, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Procesador { get; set; }

    [Display(Name = "Memoria RAM")]
    [StringLength(30, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? MemoriaRam { get; set; }

    [StringLength(30, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Almacenamiento { get; set; }

    [Display(Name = "Tamaño de pantalla")]
    [StringLength(30, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? TamanoPantalla { get; set; }

    [Display(Name = "Estado de la batería")]
    [StringLength(100, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? EstadoBateria { get; set; }

    [Display(Name = "Otros datos")]
    [StringLength(300, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Otros { get; set; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        ValidacionesNumeroSerie.Validar(TipoEquipo, NumeroSerie, nameof(NumeroSerie));
}
