using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Auditoria;

public class AuditoriaFilaViewModel
{
    public DateTime FechaHora { get; set; }

    public int? CodigoPersonal { get; set; }

    public TipoOperacionAuditoria Operacion { get; set; }

    public string Entidad { get; set; } = string.Empty;

    public int IdEntidad { get; set; }

    public string Identificador { get; set; } = string.Empty;

    public string? Detalle { get; set; }

    public string Personal => CodigoPersonal is null ? "Sistema" : $"PER-{CodigoPersonal:D4}";
}
