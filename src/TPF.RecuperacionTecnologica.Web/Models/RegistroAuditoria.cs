using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.Models;

public class RegistroAuditoria
{
    public int NroAuditoria { get; set; }

    public DateTime FechaHora { get; set; }

    public TipoOperacionAuditoria TipoOperacion { get; set; }

    public int? CodigoPersonal { get; set; }

    public Personal? Personal { get; set; }

    public string EntidadAfectada { get; set; } = string.Empty;

    public int IdEntidadAfectada { get; set; }

    public string? Detalle { get; set; }
}
