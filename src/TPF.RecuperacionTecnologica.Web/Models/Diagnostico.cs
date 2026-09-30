using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.Models;

public class Diagnostico
{
    public int NroDiagnostico { get; set; }

    public int NroEquipo { get; set; }

    public Equipo Equipo { get; set; } = null!;

    public int CodigoTecnico { get; set; }

    public Personal Tecnico { get; set; } = null!;

    public TipoDiagnostico TipoDiagnostico { get; set; }

    public DateTime FechaDiagnostico { get; set; }

    public string? DetalleFallas { get; set; }

    public string? DetalleReparacionesRealizadas { get; set; }

    public string? Observaciones { get; set; }

    public DictamenTecnico DictamenTecnico { get; set; }

    public EspecificacionesTecnicas EspecificacionesVerificadas { get; set; } = new();

    public int? DiagnosticoRectificadoId { get; set; }

    public Diagnostico? DiagnosticoRectificado { get; set; }

    public bool EsVigente { get; set; }
}
