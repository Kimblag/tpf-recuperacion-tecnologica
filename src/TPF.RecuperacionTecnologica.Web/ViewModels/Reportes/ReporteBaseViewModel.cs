namespace TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

public abstract class ReporteBaseViewModel
{
    public DateTime? Desde { get; set; }

    public DateTime? Hasta { get; set; }

    public string PeriodoConsultado => Desde is not null && Hasta is not null
        ? $"Período consultado: {Desde:dd/MM/yyyy} a {Hasta:dd/MM/yyyy}."
        : "Período consultado: todo lo disponible (acceso rápido, sin rango elegido).";
}
