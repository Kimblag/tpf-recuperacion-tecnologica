using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

public static class ValidacionesDatosUsuario
{
    public static IEnumerable<ValidationResult> Validar(
        TipoUsuario tipoUsuario, string? nombre, string? apellido, string? razonSocial,
        string? nroDocumento, string prefijoCampos = "")
    {
        if (tipoUsuario == TipoUsuario.Persona)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                yield return new ValidationResult("El nombre es obligatorio.", [prefijoCampos + "Nombre"]);
            }
            else if (!ValidacionesFormato.NombreValido(nombre))
            {
                yield return new ValidationResult("Usá solo letras, espacios, apóstrofo, guion o punto (entre 2 y 100 caracteres).", [prefijoCampos + "Nombre"]);
            }

            if (string.IsNullOrWhiteSpace(apellido))
            {
                yield return new ValidationResult("El apellido es obligatorio.", [prefijoCampos + "Apellido"]);
            }
            else if (!ValidacionesFormato.NombreValido(apellido))
            {
                yield return new ValidationResult("Usá solo letras, espacios, apóstrofo, guion o punto (entre 2 y 100 caracteres).", [prefijoCampos + "Apellido"]);
            }
        }
        else if (string.IsNullOrWhiteSpace(razonSocial))
        {
            yield return new ValidationResult("La razón social es obligatoria.", [prefijoCampos + "RazonSocial"]);
        }
        else if (!ValidacionesFormato.TextoInstitucionalValido(razonSocial))
        {
            yield return new ValidationResult("La razón social no es válida. Usá letras y, si hace falta, números y signos comunes.", [prefijoCampos + "RazonSocial"]);
        }

        var largoEsperado = tipoUsuario == TipoUsuario.Persona ? 8 : 11;

        if (nroDocumento is null || nroDocumento.Length != largoEsperado || !nroDocumento.All(char.IsAsciiDigit))
        {
            yield return new ValidationResult(
                $"El documento debe tener {largoEsperado} dígitos numéricos.",
                [prefijoCampos + "NroDocumento"]);
        }
        else if (nroDocumento.All(c => c == '0'))
        {
            yield return new ValidationResult(
                "El documento no puede estar formado solo por ceros.",
                [prefijoCampos + "NroDocumento"]);
        }
    }
}