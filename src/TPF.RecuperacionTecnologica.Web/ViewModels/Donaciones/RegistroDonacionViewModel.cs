using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

public class RegistroDonacionViewModel
{
    // los enums son nullable para que el select arranque sin opción elegida y [Required] funcione
    [Display(Name = "Tipo de equipo")]
    [Required(ErrorMessage = "El tipo de equipo es obligatorio.")]
    [EnumDataType(typeof(TipoEquipo), ErrorMessage = "Seleccione una opción válida.")]
    public TipoEquipo? TipoEquipo { get; set; }

    [Required(ErrorMessage = "La marca es obligatoria.")]
    [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
    public string Modelo { get; set; } = string.Empty;

    [Display(Name = "Estado del equipo")]
    [Required(ErrorMessage = "El estado del equipo es obligatorio.")]
    [EnumDataType(typeof(EstadoInicialDeclarado), ErrorMessage = "Seleccione una opción válida.")]
    public EstadoInicialDeclarado? EstadoInicialDeclarado { get; set; }

    [StringLength(500, ErrorMessage = "Máximo 500 caracteres.")]
    public string? Observaciones { get; set; }

    // especificaciones opcionales: arman EspecificacionesDeclaradas del Equipo
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string? Procesador { get; set; }

    [Display(Name = "Memoria RAM")]
    [StringLength(30, ErrorMessage = "Máximo 30 caracteres.")]
    public string? MemoriaRam { get; set; }

    [StringLength(30, ErrorMessage = "Máximo 30 caracteres.")]
    public string? Almacenamiento { get; set; }

    [Display(Name = "Tamaño de pantalla")]
    [StringLength(30, ErrorMessage = "Máximo 30 caracteres.")]
    public string? TamanoPantalla { get; set; }

    [Display(Name = "Estado de la batería")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string? EstadoBateria { get; set; }

    [StringLength(300, ErrorMessage = "Máximo 300 caracteres.")]
    public string? Otros { get; set; }
}
