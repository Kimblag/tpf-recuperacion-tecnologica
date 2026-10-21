using Microsoft.AspNetCore.Identity;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Diagnosticos;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.Services.Diagnosticos;

public interface IDiagnosticoService
{
    /// <summary>
    /// Lista todos los diagnósticos registrados para el personal interno, del más reciente al más antiguo,
    /// con filtros opcionales de búsqueda (número de diagnóstico o de equipo), tipo y dictamen.
    /// </summary>
    /// <returns>
    /// La página pedida, ajustada al rango válido, o vacía si ningún diagnóstico cumple los filtros.
    /// </returns>
    Task<ResultadoPaginado<DiagnosticoFilaViewModel>> ListarAsync(
        string? busqueda, TipoDiagnostico? tipo, DictamenTecnico? dictamen, int pagina, int tamanoPagina);

    /// <summary>
    /// Devuelve los datos de un diagnóstico, del equipo evaluado y del Técnico que lo registró, para consulta.
    /// </summary>
    /// <returns>Los datos del diagnóstico, o null si no existe.</returns>
    Task<DiagnosticoDetalleViewModel?> ObtenerDetalleAsync(int nroDiagnostico);

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

    /// <summary>
    /// Rectifica el diagnóstico vigente de un Equipo Disponible, En reparación o en Baja. El diagnóstico original
    /// se conserva en el historial como no vigente y la rectificación lo reemplaza. El estado del equipo se
    /// actualiza según el nuevo dictamen y el evento queda auditado con el código del Técnico.
    /// </summary>
    /// <returns>
    /// Éxito, o el motivo por el que no se pudo rectificar: el equipo no existe, no tiene un diagnóstico vigente,
    /// está Asignado, Entregado o en otro estado que no admite rectificación, o cambió mientras se registraba.
    /// En esos casos el dictamen vigente se mantiene.
    /// </returns>
    Task<IdentityResult> RegistrarRectificacionAsync(int codigoTecnico, DiagnosticoFormularioViewModel modelo);
}
