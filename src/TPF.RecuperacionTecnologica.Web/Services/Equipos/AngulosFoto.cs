namespace TPF.RecuperacionTecnologica.Web.Services.Equipos;

public static class AngulosFoto
{
    public static readonly IReadOnlyList<string> Opciones = [
        "Frontal", "Trasera", "Lateral", "Superior", "Pantalla",
        "Teclado", "Puertos", "Etiqueta", "Detalle de daño"
    ];

    public static bool EsValido(string? angulo) =>
        string.IsNullOrWhiteSpace(angulo)
        || Opciones.Contains(angulo.Trim(), StringComparer.OrdinalIgnoreCase);

    public static string? Normalizar(string? angulo)
    {
        if (string.IsNullOrWhiteSpace(angulo)) return null;

        var texto = angulo.Trim();

        return Opciones.FirstOrDefault(
            o => string.Equals(o, texto, StringComparison.OrdinalIgnoreCase))
            ?? texto;
    }
}