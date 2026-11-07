using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;

public class RestablecerContrasenaViewModel
{
    [Required(ErrorMessage = "El enlace no es válido o venció. Pedí uno nuevo.")]
    public string IdUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "El enlace no es válido o venció. Pedí uno nuevo.")]
    public string Token { get; set; } = string.Empty;

    [Display(Name = "Contraseña nueva")]
    [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
    [DataType(DataType.Password)]
    [StringLength(128, ErrorMessage = "Máximo 128 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Confirmar contraseña nueva")]
    [Required(ErrorMessage = "Confirmá la contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}