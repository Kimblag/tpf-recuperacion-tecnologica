using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

public enum AgrupacionPeriodo
{
    Mes,

    Trimestre,

    [Display(Name = "Año")]
    Anio
}
