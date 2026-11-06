using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;

public class CambiarContrasenaViewModel
{
    [Display(Name = "Contraseña actual")]
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    [DataType(DataType.Password)]
    [StringLength(128, ErrorMessage = "Máximo 128 caracteres.")]
    public string ContrasenaActual { get; set; } = string.Empty;

    [Display(Name = "Contraseña nueva")]
    [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
    [DataType(DataType.Password)]
    [StringLength(128, ErrorMessage = "Máximo 128 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Confirmar contraseña nueva")]
    [Required(ErrorMessage = "Hay que confirmar la contraseña nueva.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
