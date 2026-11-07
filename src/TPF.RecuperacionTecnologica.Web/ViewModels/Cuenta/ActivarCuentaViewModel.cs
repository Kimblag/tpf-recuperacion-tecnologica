using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;

public class ActivarCuentaViewModel
{
    [Required(ErrorMessage = "El enlace venció o no es válido.")]
    public string IdUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "El enlace venció o no es válido.")]
    public string Token { get; set; } = string.Empty;

    [Display(Name = "Contraseña")]
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [StringLength(128, ErrorMessage = "Máximo 128 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Confirmar contraseña")]
    [Required(ErrorMessage = "Confirmá la contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
