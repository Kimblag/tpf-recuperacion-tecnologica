using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

public static class ValidacionesFormato
{
    private const string CaracteresLocales = "[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]";

    private static readonly Regex PatronCorreo = new(
        "^" + CaracteresLocales + "+(?:\\." + CaracteresLocales + "+)*@(?:[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?\\.)+[A-Za-z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PatronNombre = new(
        @"^\p{L}[\p{L}\s'.\-]{1,99}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PatronTelefono = new(
        @"^[0-9\s+\-()]{6,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool CorreoValido(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
        {
            return false;
        }

        var texto = correo.Trim();
        var arroba = texto.IndexOf('@');

        return texto.Length <= 254 && arroba is > 0 and <= 64 && PatronCorreo.IsMatch(texto);
    }

    public static bool NombreValido(string? texto) =>
        !string.IsNullOrWhiteSpace(texto) && PatronNombre.IsMatch(texto.Trim());

    private static readonly Regex PatronTextoInstitucional = new(
        @"^[\p{L}\p{N}\s.,;:'’""&()/°#!¡?¿+_\-–]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TextoInstitucionalValido(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var valor = texto.Trim();

        return valor.Any(char.IsLetter) && PatronTextoInstitucional.IsMatch(valor);
    }

    public static bool TelefonoValido(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return true;
        }

        var valor = texto.Trim();

        return PatronTelefono.IsMatch(valor) && valor.Count(char.IsAsciiDigit) >= 6;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class CorreoEstrictoAttribute : ValidationAttribute
{
    public CorreoEstrictoAttribute() : base("Ingresá un correo electrónico válido, por ejemplo nombre@dominio.com.")
    {
    }

    public override bool IsValid(object? value) =>
        value is null or "" || ValidacionesFormato.CorreoValido(value as string);
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class NombrePersonaAttribute : ValidationAttribute
{
    public NombrePersonaAttribute() : base("Usá solo letras, espacios, apóstrofo, guion o punto (entre 2 y 100 caracteres).")
    {
    }

    public override bool IsValid(object? value) =>
        value is null or "" || ValidacionesFormato.NombreValido(value as string);
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class TelefonoContactoAttribute : ValidationAttribute
{
    public TelefonoContactoAttribute() : base("Ingresá un teléfono válido: solo números, espacios, +, - y paréntesis (mínimo 6 dígitos).")
    {
    }

    public override bool IsValid(object? value) =>
        value is null or "" || ValidacionesFormato.TelefonoValido(value as string);
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class TextoInstitucionalAttribute : ValidationAttribute
{
    public TextoInstitucionalAttribute() : base("Ingresá un texto válido: debe tener al menos una letra y solo puede incluir letras, números, espacios y signos comunes (. , - ' & ( ) /).")
    {
    }

    public override bool IsValid(object? value) =>
        value is null or "" || ValidacionesFormato.TextoInstitucionalValido(value as string);
}
