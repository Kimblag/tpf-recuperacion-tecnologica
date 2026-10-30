using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

public class LegajoEquipoViewModel
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

    // del donante solo se muestran el nombre y el tipo de cuenta, nunca el documento
    public string Donante { get; set; } = string.Empty;

    public TipoUsuario TipoCuentaDonante { get; set; }

    public IReadOnlyList<DiagnosticoFilaViewModel> Diagnosticos { get; set; } = [];

    public IReadOnlyList<SolicitudLegajoViewModel> Solicitudes { get; set; } = [];

    public IReadOnlyList<AsignacionLegajoViewModel> Asignaciones { get; set; } = [];

    public EntregaLegajoViewModel? Entrega { get; set; }

    public IReadOnlyList<HitoViewModel> LineaDeTiempo { get; set; } = [];

    public string CodigoEquipo => NroEquipo.ToString("D6");

    public string MarcaYModelo => $"{Marca} {Modelo}".Trim();
}
