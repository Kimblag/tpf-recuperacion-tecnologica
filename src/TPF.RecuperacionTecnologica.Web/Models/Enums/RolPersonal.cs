using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.Models.Enums;

public enum RolPersonal
{
    [Display(Name = "Técnico")]
    Tecnico,

    Coordinador,

    Administrador
}
