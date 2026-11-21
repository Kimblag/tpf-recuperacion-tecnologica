namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

public static class RangoFechasPermitido
{
    public static readonly DateTime Minima = new(2000, 1, 1);

    public static DateTime Maxima() => DateTime.UtcNow.Date.AddYears(1);

    public static bool EstaDentro(DateTime fecha) =>
        fecha.Date >= Minima && fecha.Date <= Maxima();

    public static string Mensaje() =>
        $"La fecha debe estar entre {Minima:dd/MM/yyyy} y {Maxima():dd/MM/yyyy}.";
}
