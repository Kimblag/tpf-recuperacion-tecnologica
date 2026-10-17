namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

public enum EstadoHito
{
    Completado,
    Actual,
    Pendiente,
    Cancelado
}

public class HitoViewModel
{
    public string Titulo { get; set; } = string.Empty;

    public DateTime? Fecha { get; set; }

    public string Icono { get; set; } = "check";

    public EstadoHito Estado { get; set; }

    public string? Texto { get; set; }
}