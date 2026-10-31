using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.Services.Equipos;

public class ImagenesOptions : IValidatableObject
{
    public const string Seccion = "Imagenes";
    public const string RutaPublica = "upload/equipos";

    [Range(1, 50, ErrorMessage = "El tamaño máximo debe estar entre 1 y 50 MB.")]
    public int TamanoMaximoMB { get; set; } = 10;

    [Range(1, 20, ErrorMessage = "El máximo de fotos por equipo debe estar entre 1 y 20.")]
    public int MaximoPorEquipo { get; set; } = 6;

    [Range(1, 20, ErrorMessage = "El máximo por envío debe estar entre 1 y 20.")]
    public int MaximoPorEnvio { get; set; } = 6;

    public string? CarpetaFisica { get; set; }
    public long TamanoMaximoBytes => TamanoMaximoMB * 1024L * 1024;
    public long LimiteDePeticionBytes => MaximoPorEnvio * TamanoMaximoBytes + 1024L * 1024L;

    public string ResolverCarpetaFisica(string raizWeb) => string.IsNullOrWhiteSpace(CarpetaFisica) ? Path.Combine(raizWeb, RutaPublica) :
        CarpetaFisica;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaximoPorEnvio > MaximoPorEquipo)
        {
            yield return new ValidationResult(
                "El máximo de fotos por envío no puede superar el máximo por equipo.",
                [nameof(MaximoPorEnvio)]);
        }
    }
}