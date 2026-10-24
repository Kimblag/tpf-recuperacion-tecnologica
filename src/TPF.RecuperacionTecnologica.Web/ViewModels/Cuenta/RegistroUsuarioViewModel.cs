using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

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
        foreach (var resultado in ValidacionesDatosUsuario.Validar(TipoUsuario, Nombre, Apellido, RazonSocial, NroDocumento))
        {
            yield return resultado;
        }

        var documentoEsperado = TipoUsuario == TipoUsuario.Persona ? TipoDocumento.Dni : TipoDocumento.Cuit;

        if (TipoDocumento != documentoEsperado)
        {
            yield return new ValidationResult(
                "El tipo de documento no corresponde al tipo de usuario.",
                [nameof(TipoDocumento)]);
        }
    }
}
