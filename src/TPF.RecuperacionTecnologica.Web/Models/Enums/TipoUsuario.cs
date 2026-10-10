using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.Models.Enums;

public enum TipoUsuario
{
    Persona,

    [Display(Name = "Institución")]
    Institucion
}
