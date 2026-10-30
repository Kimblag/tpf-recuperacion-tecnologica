using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Asignaciones;

public enum FiltroPlazoAsignacion
{
    Vencidas,

    [Display(Name = "Por vencer")]
    PorVencer
}
