namespace TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

public class ReporteExcepcionesViewModel : ReporteBaseViewModel
{
    public IReadOnlyList<FilaExcepcionViewModel> Filas { get; set; } = [];

    public int Total => Filas.Count;

    public TablaExportable ATabla() => new()
    {
        Titulo = "Excepciones de gestión",
        Descripcion = "Asignaciones que no siguieron la prioridad sugerida y asignaciones liberadas, con su motivo.",
        Periodo = PeriodoConsultado,
        NombreArchivo = "excepciones-de-gestion",
        Horizontal = true,
        Columnas =
        [
            new ColumnaExportable("Fecha", peso: 1.5f),
            new ColumnaExportable("Excepción", peso: 3f),
            new ColumnaExportable("Equipo", peso: 3f),
            new ColumnaExportable("Solicitud", peso: 2f),
            new ColumnaExportable("Asignó", peso: 2.5f),
            new ColumnaExportable("Justificación o motivo", peso: 5f)
        ],
        Filas = Filas
            .Select(f => (IReadOnlyList<object>)new object[]
            {
                f.Fecha, f.Tipo, f.Equipo, f.Solicitud, f.Coordinador, f.Detalle
            })
            .ToList(),
        Totales = new object[] { $"Total: {Total}", "", "", "", "", "" }
    };
}

public class FilaExcepcionViewModel
{
    public DateTime FechaUtc { get; set; }

    public string Fecha { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Equipo { get; set; } = string.Empty;

    public string Solicitud { get; set; } = string.Empty;

    public string Coordinador { get; set; } = string.Empty;

    public string Detalle { get; set; } = string.Empty;
}
