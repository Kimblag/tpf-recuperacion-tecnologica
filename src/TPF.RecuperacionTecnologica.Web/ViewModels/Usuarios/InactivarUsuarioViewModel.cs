using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

public class InactivarUsuarioViewModel
{
    public int NroUsuario { get; set; }

    [Display(Name = "Motivo de inactivación")]
    [Required(ErrorMessage = "El motivo de inactivación es obligatorio.")]
    [StringLength(300, ErrorMessage = "Máximo 300 caracteres.")]
    public string MotivoInactivacion { get; set; } = string.Empty;

    public string? NombreCompleto { get; set; }

    public string? Codigo { get; set; }
}
