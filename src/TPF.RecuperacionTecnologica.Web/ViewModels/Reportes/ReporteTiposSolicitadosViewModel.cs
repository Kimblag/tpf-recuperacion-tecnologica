using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

public class ReporteTiposSolicitadosViewModel : ReporteBaseViewModel
{
    public IReadOnlyList<TipoEquipoSolicitudesViewModel> Filas { get; set; } = [];

    public int Total => Filas.Sum(f => f.Solicitudes);

    public TablaExportable ATabla() => new()
    {
        Titulo = "Tipos de equipo más solicitados",
        Descripcion = "Ranking según la cantidad de solicitudes registradas.",
        Periodo = PeriodoConsultado,
        NombreArchivo = "tipos-mas-solicitados",
        Columnas =
        [
            new ColumnaExportable("Tipo de equipo", peso: 3f),
            new ColumnaExportable("Solicitudes", TipoColumna.Entero),
            new ColumnaExportable("Proporción", TipoColumna.Porcentaje)
        ],
        Filas = Filas
            .Select(f => (IReadOnlyList<object>)new object[]
            {
                NombreEnum.De(f.TipoEquipo),
                f.Solicitudes,
                Total == 0 ? 0d : (double)f.Solicitudes / Total
            })
            .ToList(),
        Totales = new object[] { "Total", Total, Total == 0 ? 0d : 1d }
    };
}

public class TipoEquipoSolicitudesViewModel
{
    public TipoEquipo TipoEquipo { get; set; }

    public int Solicitudes { get; set; }
}
