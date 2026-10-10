using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;

public class RegistroUsuarioViewModel : IValidatableObject
{
    [Display(Name = "Tipo de usuario")]
    [EnumDataType(typeof(TipoUsuario))]
    public TipoUsuario TipoUsuario { get; set; }

    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string? Nombre { get; set; }

    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    public string? Apellido { get; set; }

    [Display(Name = "Razón social")]
    [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
    public string? RazonSocial { get; set; }

    [Display(Name = "Tipo de documento")]
    [EnumDataType(typeof(TipoDocumento))]
    public TipoDocumento TipoDocumento { get; set; }

    [Display(Name = "Número de documento")]
    [Required(ErrorMessage = "El número de documento es obligatorio.")]
    public string NroDocumento { get; set; } = string.Empty;

    [Display(Name = "Dirección")]
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres.")]
    public string? Direccion { get; set; }

    [Display(Name = "Teléfono")]
    [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
    public string? Telefono { get; set; }

    [Display(Name = "Correo electrónico")]
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(254, ErrorMessage = "Máximo 254 caracteres.")]
    public string CorreoElectronico { get; set; } = string.Empty;

    [Display(Name = "Contraseña")]
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Confirmar contraseña")]
    [Required(ErrorMessage = "Debe confirmar la contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // se agregan reglas que no podemos usar con Data annotations, por ejemplo,
        // que el nombre y apellido sean obligatorios si es persona,
        // o que la razón social sea obligatoria si es empresa.
        if (TipoUsuario == TipoUsuario.Persona)
        {
            if (string.IsNullOrWhiteSpace(Nombre))
                // se usa yield return para devolver múltiples resultados de validación
                yield return new ValidationResult("El nombre es obligatorio.", [nameof(Nombre)]);

            if (string.IsNullOrWhiteSpace(Apellido))
                yield return new ValidationResult("El apellido es obligatorio.", [nameof(Apellido)]);
        }
        else if (string.IsNullOrWhiteSpace(RazonSocial))
        {
            yield return new ValidationResult("La razón social es obligatoria.", [nameof(RazonSocial)]);
        }

        // el tipo de documento llega desde el navegador, por eso se valida que corresponda al tipo de usuario
        var documentoEsperado = TipoUsuario == TipoUsuario.Persona ? TipoDocumento.Dni : TipoDocumento.Cuit;

        if (TipoDocumento != documentoEsperado)
        {
            yield return new ValidationResult(
                "El tipo de documento no corresponde al tipo de usuario.",
                [nameof(TipoDocumento)]);
        }

        var largoEsperado = TipoDocumento == TipoDocumento.Dni ? 8 : 11;

        if (NroDocumento.Length != largoEsperado || !NroDocumento.All(char.IsAsciiDigit))
        {
            yield return new ValidationResult(
                $"El documento debe tener {largoEsperado} dígitos numéricos.",
                [nameof(NroDocumento)]);
        }
    }
}
