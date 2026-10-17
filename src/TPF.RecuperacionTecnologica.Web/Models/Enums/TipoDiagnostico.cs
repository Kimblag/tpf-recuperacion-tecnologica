using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.Models.Enums;

public enum TipoDiagnostico
{
    [Display(Name = "Diagnóstico inicial")]
    Inicial,

    [Display(Name = "Segundo diagnóstico")]
    Segundo,

    [Display(Name = "Rectificación")]
    Rectificacion
}
