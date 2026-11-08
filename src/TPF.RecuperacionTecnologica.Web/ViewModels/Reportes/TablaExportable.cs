namespace TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

public enum TipoColumna
{
    Texto,
    Entero,
    Porcentaje
}

public enum FormatoExportacion
{
    Excel,
    Pdf
}

public class ColumnaExportable
{
    public ColumnaExportable(string titulo, TipoColumna tipo = TipoColumna.Texto)
    {
        Titulo = titulo;
        Tipo = tipo;
    }

    public string Titulo { get; }

    public TipoColumna Tipo { get; }
}

public class TablaExportable
{
    public string Titulo { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Periodo { get; set; } = string.Empty;

    public string NombreArchivo { get; set; } = string.Empty;

    public IReadOnlyList<ColumnaExportable> Columnas { get; set; } = [];

    public IReadOnlyList<IReadOnlyList<object>> Filas { get; set; } = [];

    public IReadOnlyList<object>? Totales { get; set; }
}