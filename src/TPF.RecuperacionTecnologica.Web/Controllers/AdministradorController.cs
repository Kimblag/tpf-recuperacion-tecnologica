using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Administrador;
using TPF.RecuperacionTecnologica.Web.ViewModels.Auditoria;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize]
public class AdministradorController : Controller
{
    [HttpGet]
    public IActionResult Inicio()
    {
        return View(new AdministradorInicioViewModel
        {
            UsuariosExternos = 128,
            UsuariosNuevosDelMes = 6,
            PersonalActivo = 5,
            PersonalInactivo = 1,
            Administradores = 1,
            Coordinadores = 2,
            Tecnicos = 2,
            EntregasDelMes = 24,
            VariacionEntregasPorcentaje = 12,
            EquiposPorEstado =
            [
                new() { Estado = EstadoEquipo.Disponible, Cantidad = 42 },
                new() { Estado = EstadoEquipo.PendienteRecepcion, Cantidad = 14 },
                new() { Estado = EstadoEquipo.EnReparacion, Cantidad = 18 },
                new() { Estado = EstadoEquipo.Asignado, Cantidad = 12 },
                new() { Estado = EstadoEquipo.Entregado, Cantidad = 24 },
                new() { Estado = EstadoEquipo.Baja, Cantidad = 8 }
            ],
            AuditoriaReciente =
            [
                FilaAuditoria(new DateTime(2026, 9, 19, 10, 42, 0), 12, TipoOperacionAuditoria.Diagnostico, "Diagnóstico", 219, "DIA-000219", "Rectifica DIA-000217 del equipo 000124"),
                FilaAuditoria(new DateTime(2026, 9, 19, 9, 15, 0), 7, TipoOperacionAuditoria.Solicitud, "Solicitud", 30, "SOL-000030", "Solicitud asistida registrada para Roberto Quiroga"),
                FilaAuditoria(new DateTime(2026, 9, 18, 17, 31, 0), 7, TipoOperacionAuditoria.Donacion, "Donación", 49, "DON-000049", "Donación registrada en recepción"),
                FilaAuditoria(new DateTime(2026, 9, 18, 16, 5, 0), 1, TipoOperacionAuditoria.ModificacionParametro, "Parámetro", 1, "Institucional", "Días límite de retiro: 7 → 10"),
                FilaAuditoria(new DateTime(2026, 9, 15, 15, 10, 0), 7, TipoOperacionAuditoria.Asignacion, "Asignación", 18, "ASG-000018", "Equipo 000125 asignado a SOL-000025")
            ]
        });
    }

    private static AuditoriaFilaViewModel FilaAuditoria(
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
