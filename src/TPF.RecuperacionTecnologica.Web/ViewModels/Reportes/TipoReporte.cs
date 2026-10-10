using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

public enum TipoReporte
{
    [Display(Name = "Equipos por estado")]
    EquiposPorEstado,

    [Display(Name = "Entregas por período")]
    EntregasPorPeriodo,

    [Display(Name = "Tipos de equipo más solicitados")]
    TiposSolicitados
}
