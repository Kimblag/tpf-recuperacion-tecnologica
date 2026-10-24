using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

public class EdicionUsuarioViewModel : IValidatableObject
{
    public TipoUsuario TipoUsuario { get; set; }

    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string? Nombre { get; set; }

    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string? Apellido { get; set; }

    [Display(Name = "Razón social")]
    [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
    public string? RazonSocial { get; set; }

    public TipoDocumento TipoDocumento { get; set; }

    [Display(Name = "Número de documento")]
    public string NroDocumento { get; set; } = string.Empty;

    public DateTime FechaAlta { get; set; }

    [Display(Name = "Dirección")]
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres.")]
    public string? Direccion { get; set; }

    [Display(Name = "Teléfono")]
    [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
    public string? Telefono { get; set; }

    [Display(Name = "Correo electrónico")]
    public string CorreoElectronico { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TipoUsuario == TipoUsuario.Persona)
        {
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                yield return new ValidationResult("El nombre es obligatorio.", [nameof(Nombre)]);
            }

            if (string.IsNullOrWhiteSpace(Apellido))
            {
                yield return new ValidationResult("El apellido es obligatorio.", [nameof(Apellido)]);
            }
        }
        else if (string.IsNullOrWhiteSpace(RazonSocial))
        {
            yield return new ValidationResult("La razón social es obligatoria.", [nameof(RazonSocial)]);
        }
    }
}
