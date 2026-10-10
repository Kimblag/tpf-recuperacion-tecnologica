using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Auditoria;

public class AuditoriaListadoViewModel
{
    public static readonly IReadOnlyList<string> EntidadesDisponibles =
    [
        "Donación", "Equipo", "Solicitud", "Asignación", "Entrega", "Diagnóstico", "Personal", "Parámetro"
    ];

    [DataType(DataType.Date)]
    public DateTime? Desde { get; set; }

    [DataType(DataType.Date)]
    public DateTime? Hasta { get; set; }

    public TipoOperacionAuditoria? Operacion { get; set; }

    public string? CodigoPersonal { get; set; }

    public string? Entidad { get; set; }

    public string? Identificador { get; set; }

    public IReadOnlyList<AuditoriaFilaViewModel> Filas { get; set; } = [];

    public int Total { get; set; }
}
