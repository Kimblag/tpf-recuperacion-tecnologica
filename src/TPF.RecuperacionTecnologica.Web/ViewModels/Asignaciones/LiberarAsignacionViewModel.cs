using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Asignaciones;

public class LiberarAsignacionViewModel
{
    public int NroAsignacion { get; set; }

    [Display(Name = "Motivo de la liberación")]
    [Required(ErrorMessage = "El motivo de la liberación es obligatorio.")]
    [StringLength(300, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    [MotivoSignificativo]
    public string MotivoLiberacion { get; set; } = string.Empty;
}
