using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Diagnosticos;

public class DiagnosticoDetalleViewModel
{
    public int NroDiagnostico { get; set; }

    public TipoDiagnostico TipoDiagnostico { get; set; }

    public DateTime FechaDiagnostico { get; set; }

    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public EstadoEquipo EstadoEquipo { get; set; }

    public int CodigoTecnico { get; set; }

    public string Tecnico { get; set; } = string.Empty;

    public DictamenTecnico DictamenTecnico { get; set; }

    public bool EsVigente { get; set; }

    public int? NroDiagnosticoRectificado { get; set; }

    public int? NroDiagnosticoReemplazante { get; set; }

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

    public string CodigoEquipo => NroEquipo.ToString("D6");

    public string CodigoPersonal => $"PER-{CodigoTecnico:D4}";

    public string MarcaYModelo => $"{Marca} {Modelo}".Trim();

    public bool TieneEspecificaciones =>
        !string.IsNullOrWhiteSpace(Procesador)
        || !string.IsNullOrWhiteSpace(MemoriaRam)
        || !string.IsNullOrWhiteSpace(Almacenamiento)
        || !string.IsNullOrWhiteSpace(TamanoPantalla)
        || !string.IsNullOrWhiteSpace(EstadoBateria)
        || !string.IsNullOrWhiteSpace(Otros);
}
