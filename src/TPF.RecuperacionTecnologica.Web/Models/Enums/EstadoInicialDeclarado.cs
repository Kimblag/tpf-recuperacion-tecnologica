using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.Models.Enums;

public enum EstadoInicialDeclarado
{
    Funcional,

    [Display(Name = "Con fallas")]
    ConFallas,

    [Display(Name = "Para repuestos")]
    ParaRepuestos
}
