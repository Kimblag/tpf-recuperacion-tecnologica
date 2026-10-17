using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

public class DiagnosticoVigenteViewModel
{
    public int NroDiagnostico { get; set; }

    public TipoDiagnostico TipoDiagnostico { get; set; }

    public DateTime FechaDiagnostico { get; set; }

    public string Tecnico { get; set; } = string.Empty;

    public DictamenTecnico DictamenTecnico { get; set; }

    public string? DetalleFallas { get; set; }

    public string? DetalleReparacionesRealizadas { get; set; }

    public string? Observaciones { get; set; }

    public string? Procesador { get; set; }

    public string? MemoriaRam { get; set; }

    public string? Almacenamiento { get; set; }

    public string? TamanoPantalla { get; set; }

    public string? EstadoBateria { get; set; }

    public string? Otros { get; set; }

    public string CodigoDiagnostico => $"DIA-{NroDiagnostico:D6}";
}
