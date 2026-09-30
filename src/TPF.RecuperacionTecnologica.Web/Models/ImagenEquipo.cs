namespace TPF.RecuperacionTecnologica.Web.Models;

public class ImagenEquipo
{
    public int NroImagen { get; set; }

    public int NroEquipo { get; set; }

    public Equipo Equipo { get; set; } = null!;

    public int? NroDiagnostico { get; set; }

    public Diagnostico? Diagnostico { get; set; }

    public string UrlImagen { get; set; } = string.Empty;

    public string? Angulo { get; set; }

    public DateTime FechaCarga { get; set; }
}
