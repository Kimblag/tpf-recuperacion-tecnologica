using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.Services.Configuracion;
using TPF.RecuperacionTecnologica.Web.ViewModels.Asignaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.Services.Asignaciones;

public class AsignacionService : IAsignacionService
{
    private const int MaximoJustificacion = 300;

    private const string MensajeConcurrencia =
        "La solicitud o el equipo cambiaron mientras se asignaba: otra persona pudo asignar el equipo, o la solicitud se editó o se canceló. No se guardó nada. Volver a abrir la pantalla e intentar nuevamente.";

    private readonly ApplicationDbContext _context;
    private readonly IConfiguracionService _configuracionService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<AsignacionService> _logger;

    public AsignacionService(
        ApplicationDbContext context,
        IConfiguracionService configuracionService,
        IAuditoriaService auditoriaService,
        ILogger<AsignacionService> logger)
    {
        _context = context;
        _configuracionService = configuracionService;
        _auditoriaService = auditoriaService;
        _logger = logger;
    }

    private static bool EsViolacionDeUnicidad(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };

    private static string NombreSolicitante(string? razonSocial, string? nombre, string? apellido) =>
        !string.IsNullOrWhiteSpace(razonSocial) ? razonSocial : $"{nombre} {apellido}".Trim();

    public async Task<IReadOnlyList<SolicitudCandidataViewModel>> ObtenerCandidatasAsync(int nroEquipo)
    {
        var filas = await _context.Solicitudes
            .AsNoTracking()
            .Where(s => s.NroEquipoRequerido == nroEquipo && s.EstadoSolicitud == EstadoSolicitud.Pendiente)
            .OrderBy(s => s.TipoSolicitante == TipoSolicitante.Particular ? 1 : 0)
            .ThenBy(s => s.FechaSolicitud)
            .ThenBy(s => s.NroSolicitud)
            .Select(s => new
            {
                s.NroSolicitud,
                s.TipoSolicitante,
                s.OrigenSolicitud,
                s.FechaSolicitud,
                s.Usuario.RazonSocial,
                s.Usuario.Nombre,
                s.Usuario.Apellido
            })
            .ToListAsync();

        return filas
            .Select(f => new SolicitudCandidataViewModel
            {
                NroSolicitud = f.NroSolicitud,
                Solicitante = NombreSolicitante(f.RazonSocial, f.Nombre, f.Apellido),
                TipoSolicitante = f.TipoSolicitante,
                OrigenSolicitud = f.OrigenSolicitud,
                FechaSolicitud = f.FechaSolicitud
            })
            .ToList();
    }

    public async Task<IReadOnlyList<EquipoSeleccionableViewModel>> ObtenerEquiposConSolicitudPendienteAsync(
        string? busqueda, int cantidad)
    {
        // mismo criterio de "disponible" que el catálogo: Disponible y con diagnóstico vigente
        var consulta = _context.Equipos
            .AsNoTracking()
            .Where(e => e.EstadoEquipo == EstadoEquipo.Disponible
                && _context.Diagnosticos.Any(d => d.NroEquipo == e.NroEquipo && d.EsVigente)
                && _context.Solicitudes.Any(s => s.NroEquipoRequerido == e.NroEquipo
                    && s.EstadoSolicitud == EstadoSolicitud.Pendiente));

        var texto = busqueda?.Trim();

        if (!string.IsNullOrEmpty(texto))
        {
            var esNumero = int.TryParse(texto, out var nroEquipo);

            consulta = consulta.Where(e =>
                (esNumero && e.NroEquipo == nroEquipo)
                || e.Marca.Contains(texto)
                || e.Modelo.Contains(texto));
        }

        var equipos = await consulta
            .OrderByDescending(e => e.FechaDisponibilidad)
            .ThenByDescending(e => e.NroEquipo)
            .Take(cantidad)
            .Select(e => new { e.NroEquipo, e.TipoEquipo, e.Marca, e.Modelo })
            .ToListAsync();

        return equipos
            .Select(e => new EquipoSeleccionableViewModel
            {
                NroEquipo = e.NroEquipo,
                TipoEquipo = e.TipoEquipo,
                MarcaYModelo = $"{e.Marca} {e.Modelo}"
            })
            .ToList();
    }

    public async Task<ResultadoAsignacion> AsignarAsync(
        string identityUserIdCoordinador, int nroEquipo, int nroSolicitud, string? justificacionCambio)
    {
        var codigoCoordinador = await _context.PersonalInterno
            .AsNoTracking()
            .Where(p => p.IdentityUserId == identityUserIdCoordinador
                && p.RolAsignado == RolPersonal.Coordinador
                && p.EstadoPersonal == EstadoPersonal.Activo)
            .Select(p => (int?)p.CodigoPersonal)
            .FirstOrDefaultAsync();

        if (codigoCoordinador is null)
        {
            return ResultadoAsignacion.Fallo(ErrorAsignacion.CoordinadorNoHabilitado,
                "La cuenta no está habilitada para asignar equipos.");
        }

        var justificacion = string.IsNullOrWhiteSpace(justificacionCambio) ? null : justificacionCambio.Trim();

        if (justificacion is { Length: > MaximoJustificacion })
        {
            return ResultadoAsignacion.Fallo(ErrorAsignacion.JustificacionRequerida,
                $"La justificación no puede superar los {MaximoJustificacion} caracteres.");
        }

        var configuracion = await _configuracionService.ObtenerAsync();

        if (configuracion is null)
        {
            return ResultadoAsignacion.Fallo(ErrorAsignacion.ConfiguracionNoEncontrada,
                "No se encontró la configuración institucional, necesaria para calcular el plazo de retiro.");
        }

        Asignacion asignacion;

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            // con seguimiento: el RowVersion de ambos detecta cambios concurrentes al guardar
            var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.NroEquipo == nroEquipo);

            if (equipo is null || equipo.EstadoEquipo != EstadoEquipo.Disponible)
            {
                return ResultadoAsignacion.Fallo(ErrorAsignacion.EquipoNoDisponible,
                    "El equipo no está Disponible. Solo se pueden asignar equipos disponibles.");
            }

            var solicitud = await _context.Solicitudes.FirstOrDefaultAsync(s => s.NroSolicitud == nroSolicitud);

            if (solicitud is null || solicitud.EstadoSolicitud != EstadoSolicitud.Pendiente)
            {
                return ResultadoAsignacion.Fallo(ErrorAsignacion.SolicitudNoPendiente,
                    "La solicitud ya no está Pendiente: pudo asignarse o cancelarse. Volver a abrir la pantalla.");
            }

            if (solicitud.NroEquipoRequerido != nroEquipo)
            {
                return ResultadoAsignacion.Fallo(ErrorAsignacion.SolicitudDeOtroEquipo,
                    "La solicitud elegida no pide este equipo.");
            }

            var sugerida = (await ObtenerCandidatasAsync(nroEquipo)).FirstOrDefault();
            var cambiaPrioridad = sugerida is not null && sugerida.NroSolicitud != nroSolicitud;

            if (cambiaPrioridad && justificacion is null)
            {
                return ResultadoAsignacion.Fallo(ErrorAsignacion.JustificacionRequerida,
                    "Para asignar una solicitud distinta de la sugerida hay que justificar el cambio de prioridad.");
            }

            var ahora = DateTime.UtcNow;

            asignacion = new Asignacion
            {
                NroEquipo = nroEquipo,
                NroSolicitud = nroSolicitud,
                CodigoCoordinador = codigoCoordinador.Value,
                FechaAsignacion = ahora,
                FechaLimiteRetiro = ahora.AddDays(configuracion.DiasLimiteRetiro),
                EstadoAsignacion = EstadoAsignacion.Vigente,
                JustificacionCambio = cambiaPrioridad ? justificacion : null
            };

            _context.Asignaciones.Add(asignacion);

            equipo.EstadoEquipo = EstadoEquipo.Asignado;
            equipo.FechaUltimaModificacion = ahora;
            solicitud.EstadoSolicitud = EstadoSolicitud.Asignada;
            solicitud.FechaUltimaModificacion = ahora;

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoAsignacion.Fallo(ErrorAsignacion.AsignacionConcurrente, MensajeConcurrencia);
        }
        catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
        {
            return ResultadoAsignacion.Fallo(ErrorAsignacion.AsignacionConcurrente, MensajeConcurrencia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la asignación del Equipo {NroEquipo} a la Solicitud {NroSolicitud}", nroEquipo, nroSolicitud);
            return ResultadoAsignacion.Fallo(ErrorAsignacion.Fallido,
                "No se pudo registrar la asignación. No se guardó nada. Intentar nuevamente.");
        }

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.Asignacion, codigoCoordinador, nameof(Asignacion),
                asignacion.NroAsignacion, $"Asignación del equipo {nroEquipo} a la solicitud {nroSolicitud}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar la Asignación {NroAsignacion}", asignacion.NroAsignacion);
        }

        return ResultadoAsignacion.Ok(asignacion.NroAsignacion);
    }
}
