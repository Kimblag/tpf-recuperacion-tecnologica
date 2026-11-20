namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

public static class BusquedaTexto
{
    public const string Colacion = "Latin1_General_100_CI_AI";

    public static string Patron(string termino) =>
        "%" + termino.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
}
