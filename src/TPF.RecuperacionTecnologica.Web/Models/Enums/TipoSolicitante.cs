using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.Models.Enums;

public enum TipoSolicitante
{
    Particular,
    Escuela,

    [Display(Name = "Organización social")]
    OrganizacionSocial
}
