using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;

public class RecuperarContrasenaViewModel
{
    [Display(Name = "Correo electrónico")]
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    [StringLength(254, ErrorMessage = "Máximo 254 caracteres.")]
    public string CorreoElectronico { get; set; } = string.Empty;
}