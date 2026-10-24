using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Asignaciones;

public class SolicitudCandidataViewModel
{
    public int NroSolicitud { get; set; }

    public string Solicitante { get; set; } = string.Empty;

    public TipoSolicitante TipoSolicitante { get; set; }

    public OrigenSolicitud OrigenSolicitud { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public string Codigo => $"SOL-{NroSolicitud:D6}";
}

public class AsignarEquipoViewModel
{
    public int? NroEquipo { get; set; }

    public bool EquipoDisponible { get; set; } = true;

    public TipoEquipo TipoEquipo { get; set; }

    public string MarcaYModelo { get; set; } = string.Empty;

    public IReadOnlyList<SolicitudCandidataViewModel> Candidatas { get; set; } = [];

    [Display(Name = "Solicitud a asignar")]
    [Required(ErrorMessage = "Seleccione la solicitud a asignar.")]
    public int? NroSolicitudSeleccionada { get; set; }

    [Display(Name = "Justificación del cambio de prioridad")]
    [StringLength(300, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? JustificacionCambio { get; set; }

    public string Coordinador { get; set; } = string.Empty;

    public DateTime FechaAsignacion { get; set; }

    public int DiasLimiteRetiro { get; set; }

    public DateTime FechaLimiteRetiro => FechaAsignacion.AddDays(DiasLimiteRetiro);

    public IReadOnlyList<(int NroEquipo, string Texto)> EquiposDisponibles { get; set; } = [];

    public string CodigoEquipo => NroEquipo is null ? string.Empty : NroEquipo.Value.ToString("D6");
}
