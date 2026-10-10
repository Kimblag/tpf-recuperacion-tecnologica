namespace TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

public class ReporteEntregasPorPeriodoViewModel : ReporteBaseViewModel
{
    public AgrupacionPeriodo Agrupacion { get; set; }

    public IReadOnlyList<PeriodoEntregasViewModel> Filas { get; set; } = [];

    public int Total => Filas.Sum(f => f.Cantidad);

    public int Maximo => Filas.Count == 0 ? 0 : Filas.Max(f => f.Cantidad);
}

public class PeriodoEntregasViewModel
{
    public string Etiqueta { get; set; } = string.Empty;

    public int Cantidad { get; set; }
}
