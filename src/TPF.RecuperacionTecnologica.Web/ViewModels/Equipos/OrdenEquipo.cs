using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

public enum OrdenEquipo
{
    [Display(Name = "Más antiguos primero")]
    RegistroAsc,

    [Display(Name = "Más recientes primero")]
    RegistroDesc,

    [Display(Name = "Número de equipo: de menor a mayor")]
    EquipoAsc,

    [Display(Name = "Número de equipo: de mayor a menor")]
    EquipoDesc,

    [Display(Name = "Tipo de equipo: de A a Z")]
    TipoAsc,

    [Display(Name = "Tipo de equipo: de Z a A")]
    TipoDesc,

    [Display(Name = "Marca y modelo: de A a Z")]
    ModeloAsc,

    [Display(Name = "Marca y modelo: de Z a A")]
    ModeloDesc,

    [Display(Name = "Condición declarada: de A a Z")]
    CondicionAsc,

    [Display(Name = "Condición declarada: de Z a A")]
    CondicionDesc,

    [Display(Name = "Estado: del inicio al final del proceso")]
    EstadoAsc,

    [Display(Name = "Estado: del final al inicio del proceso")]
    EstadoDesc
}
