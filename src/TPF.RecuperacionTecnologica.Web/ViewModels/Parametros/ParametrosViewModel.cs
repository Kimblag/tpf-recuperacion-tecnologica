using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Parametros;

public class ParametrosViewModel
{
    [Display(Name = "Nombre de la organización")]
    [Required(ErrorMessage = "Ingrese el nombre de la organización.")]
    [StringLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres.")]
    public string NombreOrganizacion { get; set; } = string.Empty;

    [Display(Name = "Zona de cobertura")]
    [Required(ErrorMessage = "Ingrese la zona de cobertura.")]
    [StringLength(150, ErrorMessage = "La zona no puede superar los 150 caracteres.")]
    public string ZonaCobertura { get; set; } = string.Empty;

    [Display(Name = "Días límite de retiro")]
    [Required(ErrorMessage = "Ingrese la cantidad de días.")]
    [Range(1, 365, ErrorMessage = "Ingrese un número entero entre 1 y 365.")]
    public int? DiasLimiteRetiro { get; set; }

    public int? CodigoAdministrador { get; set; }

    public DateTime? FechaUltimaModificacion { get; set; }

    public string? UltimaModificacion => FechaUltimaModificacion is null
        ? null
        : $"{FechaUltimaModificacion:dd/MM/yyyy} · PER-{CodigoAdministrador:D4}";
}
