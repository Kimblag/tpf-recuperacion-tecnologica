using System.Linq.Expressions;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

public static class OrdenListado
{
    private const string SufijoDescendente = "_desc";

    public static (string Columna, bool Descendente) Interpretar(string? orden)
    {
        if (string.IsNullOrWhiteSpace(orden))
        {
            return (string.Empty, false);
        }

        return orden.EndsWith(SufijoDescendente, StringComparison.Ordinal)
            ? (orden[..^SufijoDescendente.Length], true)
            : (orden, false);
    }

    public static string Armar(string columna, bool descendente) =>
        descendente ? columna + SufijoDescendente : columna;

    public static IQueryable<T> Ordenar<T>(
        this IQueryable<T> consulta,
        string? orden,
        IReadOnlyDictionary<string, Expression<Func<T, object>>> columnas,
        Expression<Func<T, object>> desempate,
        Func<IQueryable<T>, IOrderedQueryable<T>> predeterminado)
    {
        var (columna, descendente) = Interpretar(orden);

        if (!columnas.TryGetValue(columna, out var criterio))
        {
            return predeterminado(consulta);
        }

        var ordenada = descendente
            ? consulta.OrderByDescending(criterio)
            : consulta.OrderBy(criterio);

        return ordenada.ThenBy(desempate);
    }
}
