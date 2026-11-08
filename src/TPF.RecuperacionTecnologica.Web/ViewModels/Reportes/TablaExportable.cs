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
    public ColumnaExportable(string titulo, TipoColumna tipo = TipoColumna.Texto, float peso = 1.5f)
    {
        Titulo = titulo;
        Tipo = tipo;
        Peso = peso;
    }

    public string Titulo { get; }

    public TipoColumna Tipo { get; }

    public float Peso { get; }
}

public class TablaExportable
{
    public string Titulo { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Periodo { get; set; } = string.Empty;

    public string NombreArchivo { get; set; } = string.Empty;

    public bool Horizontal { get; set; }

    public IReadOnlyList<ColumnaExportable> Columnas { get; set; } = [];

    public IReadOnlyList<IReadOnlyList<object>> Filas { get; set; } = [];

    public IReadOnlyList<object>? Totales { get; set; }
}
