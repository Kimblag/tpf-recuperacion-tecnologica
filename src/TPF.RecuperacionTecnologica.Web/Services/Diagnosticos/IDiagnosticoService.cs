using Microsoft.AspNetCore.Identity;
using TPF.RecuperacionTecnologica.Web.ViewModels.Diagnosticos;

namespace TPF.RecuperacionTecnologica.Web.Services.Diagnosticos;

public interface IDiagnosticoService
{
    /// <summary>
    /// Registra el diagnóstico inicial de un Equipo Pendiente de recepción y actualiza su estado según el dictamen:
    /// Funcional pasa a Disponible, Necesita reparación a En reparación y No recuperable a Baja.
    /// El evento queda auditado con el código del Técnico.
    /// </summary>
    /// <returns>
    /// Éxito, o el motivo por el que no se pudo registrar: el equipo no existe, ya no está Pendiente de recepción,
    /// ya tiene un diagnóstico inicial, o cambió mientras se registraba.
    /// </returns>
    Task<IdentityResult> RegistrarInicialAsync(int codigoTecnico, DiagnosticoFormularioViewModel modelo);

    /// <summary>
    /// Registra el segundo diagnóstico de un Equipo En reparación, que reemplaza al diagnóstico vigente,
    /// y actualiza su estado según el nuevo dictamen: Funcional pasa a Disponible y No recuperable a Baja.
    /// Si el dictamen sigue siendo Necesita reparación, el equipo permanece En reparación.
    /// El detalle de reparación es obligatorio y el evento queda auditado con el código del Técnico.
    /// </summary>
    /// <returns>
    /// Éxito, o el motivo por el que no se pudo registrar: falta el detalle de reparación, el equipo no existe,
    /// ya no está En reparación, o cambió mientras se registraba.
    /// </returns>
    Task<IdentityResult> RegistrarSegundoAsync(int codigoTecnico, DiagnosticoFormularioViewModel modelo);
}
