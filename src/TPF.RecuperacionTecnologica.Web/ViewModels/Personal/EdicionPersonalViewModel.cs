using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Personal;

public class EdicionPersonalViewModel
{
    public int CodigoPersonal { get; set; }

    // el DNI se muestra pero no se edita
    [Display(Name = "DNI")]
    public string Dni { get; set; } = string.Empty;

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Apellido")]
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
    public string? Telefono { get; set; }

    [Display(Name = "Rol")]
    [EnumDataType(typeof(RolPersonal))]
    public RolPersonal RolAsignado { get; set; }

    [Display(Name = "Correo electrónico")]
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    [StringLength(254, ErrorMessage = "Máximo 254 caracteres.")]
    public string CorreoElectronico { get; set; } = string.Empty;

    public EstadoPersonal EstadoPersonal { get; set; }

    public bool EsRaiz { get; set; }
    public string Codigo => $"PER-{CodigoPersonal:D4}";

    public string NombreCompleto => $"{Nombre} {Apellido}";
}
