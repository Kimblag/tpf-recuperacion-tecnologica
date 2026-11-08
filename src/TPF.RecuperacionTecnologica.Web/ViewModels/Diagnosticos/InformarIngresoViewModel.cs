using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Diagnosticos;

public class InformarIngresoViewModel
{
    [Display(Name = "Número de equipo")]
    [Range(1, int.MaxValue, ErrorMessage = "Ingresá un número de equipo válido.")]
    public int? NroEquipo { get; set; }

    [Display(Name = "Qué llegó al taller")]
    [Required(ErrorMessage = "Describí qué equipo llegó al taller.")]
    [StringLength(400, ErrorMessage = "Máximo 400 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;
}
