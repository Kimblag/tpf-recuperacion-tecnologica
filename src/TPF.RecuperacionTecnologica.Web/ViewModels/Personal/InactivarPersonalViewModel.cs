using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Personal;

public class InactivarPersonalViewModel
{
    public int CodigoPersonal { get; set; }

    [Display(Name = "Motivo de inactivación")]
    [Required(ErrorMessage = "El motivo de inactivación es obligatorio.")]
    [StringLength(300, ErrorMessage = "Máximo 300 caracteres.")]
    public string MotivoInactivacion { get; set; } = string.Empty;

    // solo para mostrar en el modal
    public string? NombreCompleto { get; set; }

    public string? Codigo { get; set; }
}
