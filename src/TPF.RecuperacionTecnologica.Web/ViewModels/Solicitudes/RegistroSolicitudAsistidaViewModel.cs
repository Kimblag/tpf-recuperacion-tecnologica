using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

public class RegistroSolicitudAsistidaViewModel
{
    public SelectorUsuarioViewModel Usuario { get; set; } = new();

    [Display(Name = "Tipo de solicitante")]
    [Required(ErrorMessage = "El tipo de solicitante es obligatorio.")]
    [EnumDataType(typeof(TipoSolicitante), ErrorMessage = "Seleccione un tipo de solicitante válido.")]
    public TipoSolicitante? TipoSolicitante { get; set; }

    [Display(Name = "Equipo solicitado")]
    [Required(ErrorMessage = "El equipo solicitado es obligatorio.")]
    public int? NroEquipo { get; set; }

    [Display(Name = "Motivo de la solicitud")]
    [Required(ErrorMessage = "El motivo de la solicitud es obligatorio.")]
    [StringLength(500, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string MotivoSolicitud { get; set; } = string.Empty;

    public string Coordinador { get; set; } = string.Empty;

    public DateTime FechaSolicitud { get; set; }

    public IReadOnlyList<(int NroEquipo, string Texto)> EquiposDisponibles { get; set; } = [];
}
