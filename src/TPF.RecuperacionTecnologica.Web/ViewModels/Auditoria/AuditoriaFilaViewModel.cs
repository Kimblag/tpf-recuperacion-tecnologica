using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Auditoria;

public class AuditoriaFilaViewModel
{
    public DateTime FechaHora { get; set; }

    public int? CodigoPersonal { get; set; }

    public string? NombrePersonal { get; set; }

    public TipoOperacionAuditoria Operacion { get; set; }

    public string Entidad { get; set; } = string.Empty;

    public int IdEntidad { get; set; }

    public string Identificador { get; set; } = string.Empty;

    public string? Detalle { get; set; }

    // sin código de personal, la acción la hizo la propia persona (donación, solicitud, edición o cancelación)
    public string Personal => CodigoPersonal is null
        ? "Autoservicio"
        : string.IsNullOrWhiteSpace(NombrePersonal)
            ? $"PER-{CodigoPersonal:D4}"
            : $"{NombrePersonal} (PER-{CodigoPersonal:D4})";

    public string EtiquetaEntidad => AuditoriaListadoViewModel.EtiquetaEntidad(Entidad);

    public static string IdentificadorDe(string entidad, int id) => entidad switch
    {
        "Solicitud" => $"SOL-{id:D6}",
        "Asignacion" => $"ASG-{id:D6}",
        "Entrega" => $"ENT-{id:D6}",
        "Diagnostico" => $"DIA-{id:D6}",
        "Personal" => $"PER-{id:D4}",
        _ => id.ToString("D6")
    };
}
