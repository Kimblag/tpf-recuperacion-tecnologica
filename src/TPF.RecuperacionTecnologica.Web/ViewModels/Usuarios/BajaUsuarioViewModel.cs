using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

public class BajaUsuarioViewModel
{
    [Display(Name = "Motivo de la baja")]
    [Required(ErrorMessage = "El motivo de la baja es obligatorio.")]
    [StringLength(300, ErrorMessage = "Máximo 300 caracteres.")]
    public string MotivoBaja { get; set; } = string.Empty;
}
