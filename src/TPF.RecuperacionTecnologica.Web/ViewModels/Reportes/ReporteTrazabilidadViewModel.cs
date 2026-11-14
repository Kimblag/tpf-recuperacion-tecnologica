namespace TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

public class ReporteTrazabilidadViewModel : ReporteBaseViewModel
{
    public IReadOnlyList<FilaTrazabilidadViewModel> Filas { get; set; } = [];

    public int Total => Filas.Count;

    public TablaExportable ATabla() => new()
    {
        Titulo = "Trazabilidad por equipo",
        Descripcion = "Recorrido de cada equipo: quién lo donó, diagnosticó, asignó y entregó, y a qué tipo de beneficiario.",
        Periodo = PeriodoConsultado,
        NombreArchivo = "trazabilidad-por-equipo",
        Horizontal = true,
        Columnas =
        [
            new ColumnaExportable("Equipo", peso: 3f),
            new ColumnaExportable("Estado", peso: 2f),
            new ColumnaExportable("Registro", peso: 1.5f),
            new ColumnaExportable("Donante", peso: 2.5f),
            new ColumnaExportable("Diagnóstico", peso: 3f),
            new ColumnaExportable("Asignación", peso: 3f),
            new ColumnaExportable("Beneficiario", peso: 3f),
            new ColumnaExportable("Entrega", peso: 3f),
            new ColumnaExportable("Retiró", peso: 2.5f)
        ],
        Filas = Filas
            .Select(f => (IReadOnlyList<object>)new object[]
            {
                f.Equipo, f.Estado, f.FechaRegistro, f.Donante, f.Diagnostico, f.Asignacion, f.Beneficiario, f.Entrega, f.Retiro
            })
            .ToList(),
        Totales = new object[] { $"Total: {Total}", "", "", "", "", "", "", "", "" }
    };
}

public class FilaTrazabilidadViewModel
{
    public string Equipo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string FechaRegistro { get; set; } = string.Empty;

    public string Donante { get; set; } = string.Empty;

    public string Diagnostico { get; set; } = string.Empty;

    public string Asignacion { get; set; } = string.Empty;

    public string Beneficiario { get; set; } = string.Empty;

    public string Entrega { get; set; } = string.Empty;

    public string Retiro { get; set; } = string.Empty;
}
