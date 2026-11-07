using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

// un solo dato para todos los equipos: IMEI en los celulares y número de serie en el resto
public static partial class ValidacionesNumeroSerie
{
    public const int LargoMaximo = 50;

    public const string MensajeDuplicado = "Ya hay un equipo registrado con ese número.";

    // nombre que EF Core le da al índice único de Equipos.NumeroSerie; sirve para distinguir este choque de otros
    public const string NombreIndice = "IX_Equipos_NumeroSerie";

    [GeneratedRegex("^[A-Z0-9-]{3,50}$")]
    private static partial Regex FormatoSerie();

    public static string EtiquetaPara(TipoEquipo? tipo) =>
        tipo == TipoEquipo.Celular ? "IMEI" : "Número de serie";

    // se guarda sin espacios en los extremos y en mayúsculas; vacío se guarda como null
    public static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim().ToUpperInvariant();

    public static IEnumerable<ValidationResult> Validar(TipoEquipo? tipo, string? valor, string nombreCampo)
    {
        var numero = Normalizar(valor);

        // es opcional: sin dato no hay nada que validar
        if (numero is null)
        {
            yield break;
        }

        if (tipo == TipoEquipo.Celular)
        {
            if (numero.Length != 15 || !numero.All(char.IsAsciiDigit))
            {
                yield return new ValidationResult("El IMEI debe tener 15 dígitos numéricos.", [nombreCampo]);
            }
            else if (!CumpleLuhn(numero))
            {
                yield return new ValidationResult(
                    "El IMEI no es válido. Revisá que los 15 dígitos estén bien copiados.", [nombreCampo]);
            }

            yield break;
        }

        if (!FormatoSerie().IsMatch(numero))
        {
            yield return new ValidationResult(
                $"El número de serie debe tener entre 3 y {LargoMaximo} caracteres: letras, números o guiones.",
                [nombreCampo]);
        }
    }

    // algoritmo de Luhn: desde la derecha se duplica uno de cada dos dígitos y la suma total debe ser múltiplo de 10
    private static bool CumpleLuhn(string digitos)
    {
        var suma = 0;

        for (var i = 0; i < digitos.Length; i++)
        {
            var digito = digitos[digitos.Length - 1 - i] - '0';

            if (i % 2 == 1)
            {
                digito *= 2;

                if (digito > 9)
                {
                    digito -= 9;
                }
            }

            suma += digito;
        }

        return suma % 10 == 0;
    }
}
