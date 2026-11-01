namespace TPF.RecuperacionTecnologica.Web.Services.Equipos;

public record ArchivoImagen(IFormFile Archivo, string? Angulo)
{
    public static List<ArchivoImagen> Combinar(IEnumerable<IFormFile> archivos, IReadOnlyList<string?> angulos) =>
        archivos
            .Select((archivo, indice) => new ArchivoImagen(archivo, indice < angulos.Count ? angulos[indice] : null))
            .ToList();
}