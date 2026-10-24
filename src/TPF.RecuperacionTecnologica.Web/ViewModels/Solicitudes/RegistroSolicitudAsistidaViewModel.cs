using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

public class RegistroSolicitudAsistidaViewModel : IValidatableObject
{
    public SelectorUsuarioViewModel Usuario { get; set; } = new();

    [Display(Name = "Tipo de solicitante")]
    [Required(ErrorMessage = "El tipo de solicitante es obligatorio.")]
    [EnumDataType(typeof(TipoSolicitante), ErrorMessage = "Seleccione un tipo de solicitante válido.")]
    public TipoSolicitante? TipoSolicitante { get; set; }

    [Display(Name = "Equipo solicitado")]
    [Required(ErrorMessage = "Buscar y elegir el equipo solicitado.")]
    public int? NroEquipo { get; set; }

    [Display(Name = "Motivo de la solicitud")]
    [Required(ErrorMessage = "El motivo de la solicitud es obligatorio.")]
    [StringLength(500, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string MotivoSolicitud { get; set; } = string.Empty;

    public string Coordinador { get; set; } = string.Empty;

    public DateTime FechaSolicitud { get; set; }

    public SelectorEquipoViewModel Equipo { get; set; } = new();

    public string? ImpedimentoTitulo { get; set; }

    public string? ImpedimentoMensaje { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Usuario.Modo == ModoUsuarioAsistido.Existente)
        {
            if (Usuario.NroUsuario is null)
            {
                yield return new ValidationResult(
                    "Buscar y seleccionar un usuario activo antes de registrar la solicitud.",
                    [$"{nameof(Usuario)}.{nameof(Usuario.Busqueda)}"]);
            }

            yield break;
        }

        var correo = Usuario.CorreoElectronico?.Trim();

        if (string.IsNullOrEmpty(correo))
        {
            yield return new ValidationResult(
                "El correo electrónico es obligatorio.",
                [$"{nameof(Usuario)}.{nameof(Usuario.CorreoElectronico)}"]);
        }
        else if (!new EmailAddressAttribute().IsValid(correo))
        {
            yield return new ValidationResult(
                "El correo electrónico no es válido.",
                [$"{nameof(Usuario)}.{nameof(Usuario.CorreoElectronico)}"]);
        }

        foreach (var resultado in ValidacionesDatosUsuario.Validar(
            Usuario.TipoUsuario, Usuario.Nombre, Usuario.Apellido, Usuario.RazonSocial,
            Usuario.NroDocumento, $"{nameof(Usuario)}."))
        {
            yield return resultado;
        }
    }
}