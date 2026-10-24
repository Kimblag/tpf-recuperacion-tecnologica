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

            if (string.IsNullOrWhiteSpace(apellido))
            {
                yield return new ValidationResult("El apellido es obligatorio.", [prefijoCampos + "Apellido"]);
            }
        }
        else if (string.IsNullOrWhiteSpace(razonSocial))
        {
            yield return new ValidationResult("La razón social es obligatoria.", [prefijoCampos + "RazonSocial"]);
        }

        var largoEsperado = tipoUsuario == TipoUsuario.Persona ? 8 : 11;

        if (nroDocumento is null || nroDocumento.Length != largoEsperado || !nroDocumento.All(char.IsAsciiDigit))
        {
            yield return new ValidationResult(
                $"El documento debe tener {largoEsperado} dígitos numéricos.",
                [prefijoCampos + "NroDocumento"]);
        }
    }
}