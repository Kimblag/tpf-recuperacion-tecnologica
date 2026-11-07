namespace TPF.RecuperacionTecnologica.Web.Services.Reloj;

public static class RelojInstitucionalExtensions
{
    public static string Fecha(this IRelojInstitucional reloj, DateTime instanteUtc) =>
        reloj.ALocal(instanteUtc).ToString("dd/MM/yyyy");

    public static string Fecha(this IRelojInstitucional reloj, DateTime? instanteUtc, string vacio = "—") =>
        instanteUtc is null ? vacio : reloj.Fecha(instanteUtc.Value);

    public static string FechaHora(this IRelojInstitucional reloj, DateTime instanteUtc) =>
        reloj.ALocal(instanteUtc).ToString("dd/MM/yyyy HH:mm");
}