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
}
