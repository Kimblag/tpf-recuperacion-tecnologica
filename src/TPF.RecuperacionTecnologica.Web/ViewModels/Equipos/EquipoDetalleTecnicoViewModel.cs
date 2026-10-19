using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

public class EquipoDetalleTecnicoViewModel
{
    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public EstadoInicialDeclarado EstadoInicialDeclarado { get; set; }

    public string? Observaciones { get; set; }

    public EstadoEquipo EstadoEquipo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaDisponibilidad { get; set; }

    public DateTime? FechaCancelacion { get; set; }

    public string? MotivoCancelacion { get; set; }

    public string Donante { get; set; } = string.Empty;

    public string? Procesador { get; set; }

    public string? MemoriaRam { get; set; }

    public string? Almacenamiento { get; set; }

    public string? TamanoPantalla { get; set; }

    public string? EstadoBateria { get; set; }

    public string? Otros { get; set; }

    public DiagnosticoVigenteViewModel? DiagnosticoVigente { get; set; }

    public string CodigoEquipo => NroEquipo.ToString("D6");

    public string MarcaYModelo => $"{Marca} {Modelo}".Trim();

    public bool PuedeRegistrarDiagnostico => EstadoEquipo == EstadoEquipo.PendienteRecepcion;

    public bool PuedeRegistrarSegundo => EstadoEquipo == EstadoEquipo.EnReparacion;

    public bool PuedeRectificar => DiagnosticoVigente is not null
        && EstadoEquipo is EstadoEquipo.Disponible or EstadoEquipo.EnReparacion or EstadoEquipo.Baja;

    public bool AdmiteAcciones => PuedeRegistrarDiagnostico || PuedeRegistrarSegundo || PuedeRectificar;

    public bool TieneEspecificaciones =>
        !string.IsNullOrWhiteSpace(Procesador)
        || !string.IsNullOrWhiteSpace(MemoriaRam)
        || !string.IsNullOrWhiteSpace(Almacenamiento)
        || !string.IsNullOrWhiteSpace(TamanoPantalla)
        || !string.IsNullOrWhiteSpace(EstadoBateria)
        || !string.IsNullOrWhiteSpace(Otros);
}
