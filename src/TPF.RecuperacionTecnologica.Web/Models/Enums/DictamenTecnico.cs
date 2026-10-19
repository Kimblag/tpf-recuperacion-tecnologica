using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.Models.Enums;

public enum DictamenTecnico
{
    Funcional,

    [Display(Name = "Necesita reparación")]
    NecesitaReparacion,

    [Display(Name = "No recuperable")]
    NoRecuperable
}
