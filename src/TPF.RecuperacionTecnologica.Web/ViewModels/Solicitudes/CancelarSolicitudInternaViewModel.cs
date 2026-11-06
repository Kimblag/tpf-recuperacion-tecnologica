using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

public class CancelarSolicitudInternaViewModel
{
    [Display(Name = "Motivo de la cancelación")]
    [Required(ErrorMessage = "El motivo de la cancelación es obligatorio.")]
    [StringLength(300, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string MotivoCancelacion { get; set; } = string.Empty;
}
