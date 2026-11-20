using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Personal;

public class AltaPersonalViewModel
{
    [Display(Name = "DNI")]
    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener 8 dígitos numéricos.")]
    public string Dni { get; set; } = string.Empty;

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [NombrePersona]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Apellido")]
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [NombrePersona]
    public string Apellido { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    [TelefonoContacto]
    public string? Telefono { get; set; }

    [Display(Name = "Rol")]
    [Required(ErrorMessage = "Elegí un rol.")]
    [EnumDataType(typeof(RolPersonal), ErrorMessage = "Elegí un rol.")]
    public RolPersonal? RolAsignado { get; set; }

    [Display(Name = "Correo electrónico")]
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [CorreoEstricto]
    [StringLength(254, ErrorMessage = "Máximo 254 caracteres.")]
    public string CorreoElectronico { get; set; } = string.Empty;

    [Display(Name = "Contraseña inicial")]
    [Required(ErrorMessage = "La contraseña inicial es obligatoria.")]
    [DataType(DataType.Password)]
    [StringLength(128, ErrorMessage = "Máximo 128 caracteres.")]
    public string Password { get; set; } = string.Empty;
}
