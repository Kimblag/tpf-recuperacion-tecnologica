using TPF.RecuperacionTecnologica.Web.ViewModels.Administrador;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

public class ReporteEquiposPorEstadoViewModel : ReporteBaseViewModel
{
    public IReadOnlyList<EquipoEstadoCantidadViewModel> Estados { get; set; } = [];

    public int Total => Estados.Sum(e => e.Cantidad);

    public TablaExportable ATabla() => new()
    {
        Titulo = "Equipos por estado",
        Descripcion = "Situación actual del inventario.",
        Periodo = PeriodoConsultado,
        NombreArchivo = "equipos-por-estado",
        Columnas =
        [
            new ColumnaExportable("Estado", peso: 3f),
            new ColumnaExportable("Cantidad", TipoColumna.Entero),
            new ColumnaExportable("Proporción", TipoColumna.Porcentaje)
        ],
        Filas = Estados
            .Select(e => (IReadOnlyList<object>)new object[]
            {
                NombreEnum.De(e.Estado),
                e.Cantidad,
                Total == 0 ? 0d : (double)e.Cantidad / Total
            })
            .ToList(),
        Totales = new object[] { "Total", Total, Total == 0 ? 0d : 1d }
    };
}