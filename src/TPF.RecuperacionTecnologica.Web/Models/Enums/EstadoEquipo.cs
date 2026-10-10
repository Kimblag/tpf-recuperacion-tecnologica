using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.Models.Enums;

public enum EstadoEquipo
{
    [Display(Name = "Pendiente de recepción")]
    PendienteRecepcion,

    Disponible,

    [Display(Name = "En reparación")]
    EnReparacion,

    Asignado,

    Entregado,

    Baja,

    Cancelado
}
