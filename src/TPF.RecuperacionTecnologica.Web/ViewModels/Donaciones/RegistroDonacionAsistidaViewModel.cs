using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

public class RegistroDonacionAsistidaViewModel : RegistroDonacionViewModel
{
    public SelectorUsuarioViewModel Usuario { get; set; } = new();

    public string Coordinador { get; set; } = string.Empty;

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // primero las validaciones del equipo (número de serie o IMEI)
        foreach (var resultado in base.Validate(validationContext))
        {
            yield return resultado;
        }

        if (Usuario.Modo == ModoUsuarioAsistido.Existente)
        {
            if (Usuario.NroUsuario is null)
            {
                yield return new ValidationResult(
                    "Buscá y seleccioná un usuario activo antes de registrar la donación.",
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
