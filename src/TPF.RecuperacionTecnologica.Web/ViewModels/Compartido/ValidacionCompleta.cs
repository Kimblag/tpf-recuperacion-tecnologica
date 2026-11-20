using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

// ASP.NET ejecuta Validate (IValidatableObject) solo si pasaron las validaciones de cada campo.
// Si alguna falló, esto agrega igual los errores de Validate, para que se vean todos en el primer envío.
public static class ValidacionCompleta
{
    public static void AgregarErroresDeValidate(this ModelStateDictionary modelState, IValidatableObject modelo)
    {
        if (modelState.IsValid)
        {
            return;
        }

        foreach (var resultado in modelo.Validate(new ValidationContext(modelo)))
        {
            var campos = resultado.MemberNames.Any() ? resultado.MemberNames : new[] { string.Empty };

            foreach (var campo in campos)
            {
                // si el campo ya tiene un error de su propia validación, se deja solo ese
                if (modelState.TryGetValue(campo, out var entrada) && entrada.Errors.Count > 0)
                {
                    continue;
                }

                modelState.AddModelError(campo, resultado.ErrorMessage ?? string.Empty);
            }
        }
    }
}
