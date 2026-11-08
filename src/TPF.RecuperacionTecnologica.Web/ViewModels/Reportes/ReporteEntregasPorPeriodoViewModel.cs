using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

public class ReporteEntregasPorPeriodoViewModel : ReporteBaseViewModel
{
    public AgrupacionPeriodo Agrupacion { get; set; }

    public IReadOnlyList<PeriodoEntregasViewModel> Filas { get; set; } = [];

    public int Total => Filas.Sum(f => f.Cantidad);

    public int Maximo => Filas.Count == 0 ? 0 : Filas.Max(f => f.Cantidad);

    public TablaExportable ATabla() => new()
    {
        Titulo = "Entregas por período",
        Descripcion = $"Equipos entregados, agrupados por {NombreEnum.De(Agrupacion).ToLowerInvariant()}.",
        Periodo = PeriodoConsultado,
        NombreArchivo = "entregas-por-periodo",
        Columnas =
        [
            new ColumnaExportable("Período", peso: 3f),
            new ColumnaExportable("Entregas", TipoColumna.Entero)
        ],
        Filas = Filas
            .Select(f => (IReadOnlyList<object>)new object[] { f.Etiqueta, f.Cantidad })
            .ToList(),
        Totales = new object[] { "Total", Total }
    };
}

public class PeriodoEntregasViewModel
{
    public string Etiqueta { get; set; } = string.Empty;

    public int Cantidad { get; set; }
}
