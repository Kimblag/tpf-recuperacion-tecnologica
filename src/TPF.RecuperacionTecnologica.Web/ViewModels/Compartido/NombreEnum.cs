using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

public static class NombreEnum
{
    public static string De(Enum valor) =>
        valor.GetType().GetMember(valor.ToString())[0]
            .GetCustomAttribute<DisplayAttribute>()?.Name
            ?? valor.ToString();
}