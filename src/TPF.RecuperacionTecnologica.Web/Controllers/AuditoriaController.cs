using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Auditoria;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize(Policy = "SoloAdministrador")]
public class AuditoriaController : Controller
{
    [HttpGet]
    public IActionResult Index(AuditoriaListadoViewModel filtros)
    {
        // TODO: reemplazar estos datos de ejemplo por la consulta real de auditoría con filtros y paginación
        filtros.Filas =
        [
            Fila(new DateTime(2026, 9, 19, 10, 42, 0), 12, TipoOperacionAuditoria.Diagnostico, "Diagnóstico", 219, "DIA-000219", "Rectifica DIA-000217 del equipo 000124"),
            Fila(new DateTime(2026, 9, 19, 9, 15, 0), 7, TipoOperacionAuditoria.Solicitud, "Solicitud", 30, "SOL-000030", "Solicitud asistida registrada para Roberto Quiroga"),
            Fila(new DateTime(2026, 9, 18, 17, 31, 0), 7, TipoOperacionAuditoria.Donacion, "Donación", 49, "DON-000049", "Donación registrada en recepción"),
            Fila(new DateTime(2026, 9, 18, 16, 5, 0), 1, TipoOperacionAuditoria.ModificacionParametro, "Parámetro", 1, "Institucional", "Días límite de retiro: 7 → 10"),
            Fila(new DateTime(2026, 9, 17, 12, 20, 0), 9, TipoOperacionAuditoria.Donacion, "Equipo", 127, "000127", "Equipo registrado por la donación DON-000048"),
            Fila(new DateTime(2026, 9, 16, 11, 48, 0), 12, TipoOperacionAuditoria.Diagnostico, "Diagnóstico", 217, "DIA-000217", "Segundo diagnóstico del equipo 000124"),
            Fila(new DateTime(2026, 9, 15, 15, 10, 0), 7, TipoOperacionAuditoria.Asignacion, "Asignación", 18, "ASG-000018", "Equipo 000125 asignado a SOL-000025"),
            Fila(new DateTime(2026, 9, 15, 9, 2, 0), 1, TipoOperacionAuditoria.AltaPersonal, "Personal", 14, "PER-0014", "Personal dado de alta con rol Técnico"),
            Fila(new DateTime(2026, 9, 12, 14, 37, 0), 1, TipoOperacionAuditoria.InactivacionPersonal, "Personal", 15, "PER-0015", "Personal inactivado"),
            Fila(new DateTime(2026, 9, 10, 10, 0, 0), 7, TipoOperacionAuditoria.Solicitud, "Solicitud", 20, "SOL-000020", "Solicitud cancelada por el solicitante")
        ];
        filtros.Total = 248;

        return View(filtros);
    }

    private static AuditoriaFilaViewModel Fila(
        DateTime fecha, int codigoPersonal, TipoOperacionAuditoria operacion,
        string entidad, int idEntidad, string identificador, string detalle) => new()
        {
            FechaHora = fecha,
            CodigoPersonal = codigoPersonal,
            Operacion = operacion,
            Entidad = entidad,
            IdEntidad = idEntidad,
            Identificador = identificador,
            Detalle = detalle
        };
}
