using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

public class EditarMotivoSolicitudViewModel
{
    public int NroSolicitud { get; set; }

    [Display(Name = "Motivo de la solicitud")]
    [Required(ErrorMessage = "El motivo de la solicitud es obligatorio.")]
    [StringLength(500, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string MotivoSolicitud { get; set; } = string.Empty;

    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string MarcaYModelo { get; set; } = string.Empty;

    public string Solicitante { get; set; } = string.Empty;

    public TipoSolicitante TipoSolicitante { get; set; }

    public OrigenSolicitud OrigenSolicitud { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public EstadoSolicitud EstadoSolicitud { get; set; }

    public string Codigo => $"SOL-{NroSolicitud:D6}";

    public string CodigoEquipo => NroEquipo.ToString("D6");

    public bool PuedeEditarse => EstadoSolicitud == EstadoSolicitud.Pendiente;
}