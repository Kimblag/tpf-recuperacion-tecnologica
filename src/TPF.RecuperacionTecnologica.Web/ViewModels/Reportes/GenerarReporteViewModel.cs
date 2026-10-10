using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

public class GenerarReporteViewModel : IValidatableObject
{
    [Display(Name = "Fecha desde")]
    [Required(ErrorMessage = "Ingrese la fecha desde.")]
    [DataType(DataType.Date)]
    public DateTime? Desde { get; set; }

    [Display(Name = "Fecha hasta")]
    [Required(ErrorMessage = "Ingrese la fecha hasta.")]
    [DataType(DataType.Date)]
    public DateTime? Hasta { get; set; }

    [Display(Name = "Tipo de informe")]
    [Required(ErrorMessage = "Elija el tipo de informe.")]
    public TipoReporte? Tipo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Desde is not null && Hasta is not null && Desde > Hasta)
        {
            yield return new ValidationResult(
                "La fecha hasta no puede ser anterior a la fecha desde.",
                [nameof(Hasta)]);
        }
    }
}
