using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

namespace TPF.RecuperacionTecnologica.Web.Services.Solicitudes;

public class SolicitudService : ISolicitudService
{
    // TODO: pasar el máximo de solicitudes pendientes a un parámetro institucional
    public const int MaximoPendientesPorUsuario = 3;

    private const string MensajeUsuarioNoHabilitado = "La cuenta no está habilitada para registrar solicitudes.";
    private const string MensajeSolicitudDuplicada = "Esta cuenta ya tiene una solicitud pendiente para este equipo. Su estado se puede ver en Mis solicitudes.";
    private const string MotivoCancelacionSolicitante = "Cancelada por la persona solicitante.";

    private static IdentityResult Fallo(string codigo, string descripcion) =>
        IdentityResult.Failed(new IdentityError { Code = codigo, Description = descripcion });

    private readonly ApplicationDbContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<SolicitudService> _logger;

    public SolicitudService(
        ApplicationDbContext context,
        IAuditoriaService auditoriaService,
        ILogger<SolicitudService> logger)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _logger = logger;
    }

    private static bool TipoCoherente(TipoUsuario tipoUsuario, TipoSolicitante tipoSolicitante) =>
        tipoUsuario == TipoUsuario.Persona
            ? tipoSolicitante == TipoSolicitante.Particular
            : tipoSolicitante is TipoSolicitante.Escuela or TipoSolicitante.OrganizacionSocial;

    private static bool EsViolacionDeUnicidad(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };

    private async Task<Usuario?> ObtenerUsuarioHabilitadoAsync(string identityUserId)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);

        return usuario is not null && usuario.EstadoUsuario == EstadoUsuario.Activo ? usuario : null;
    }

    private async Task<ImpedimentoSolicitud?> EvaluarAsync(Usuario usuario, int nroEquipo)
    {
        var equipo = await _context.Equipos
            .AsNoTracking()
            .Where(e => e.NroEquipo == nroEquipo)
            .Select(e => new { e.EstadoEquipo, e.NroUsuarioDonante })
            .FirstOrDefaultAsync();

        if (equipo is null || equipo.EstadoEquipo != EstadoEquipo.Disponible)
        {
            return new ImpedimentoSolicitud(ErrorRegistroSolicitud.EquipoNoDisponible,
                "El equipo ya no está disponible.");
        }

        if (equipo.NroUsuarioDonante == usuario.NroUsuario)
        {
            return new ImpedimentoSolicitud(ErrorRegistroSolicitud.EquipoPropio,
                "Este equipo fue donado por esta misma cuenta, por eso no puede solicitarse.");
        }

        var equiposPendientes = await _context.Solicitudes
            .AsNoTracking()
            .Where(s => s.NroUsuario == usuario.NroUsuario && s.EstadoSolicitud == EstadoSolicitud.Pendiente)
            .Select(s => s.NroEquipoRequerido)
            .ToListAsync();

        if (equiposPendientes.Contains(nroEquipo))
        {
            return new ImpedimentoSolicitud(ErrorRegistroSolicitud.SolicitudDuplicada, MensajeSolicitudDuplicada);
        }

        if (equiposPendientes.Count >= MaximoPendientesPorUsuario)
        {
            return new ImpedimentoSolicitud(ErrorRegistroSolicitud.LimiteAlcanzado,
                $"Esta cuenta ya tiene {MaximoPendientesPorUsuario} solicitudes pendientes, que es el máximo. Hay que esperar a que alguna se resuelva o cancelarla para pedir otro equipo.");
        }

        return null;
    }

    public async Task<ImpedimentoSolicitud?> ObtenerImpedimentoAsync(string identityUserId, int nroEquipo)
    {
        var usuario = await ObtenerUsuarioHabilitadoAsync(identityUserId);

        if (usuario is null)
        {
            return new ImpedimentoSolicitud(ErrorRegistroSolicitud.UsuarioNoHabilitado, MensajeUsuarioNoHabilitado);
        }

        return await EvaluarAsync(usuario, nroEquipo);
    }

    public async Task<ResultadoRegistroSolicitud> RegistrarAsync(string identityUserId, FormularioSolicitudViewModel modelo)
    {
        var usuario = await ObtenerUsuarioHabilitadoAsync(identityUserId);

        if (usuario is null)
        {
            return ResultadoRegistroSolicitud.Fallo(ErrorRegistroSolicitud.UsuarioNoHabilitado, MensajeUsuarioNoHabilitado);
        }

        if (modelo.TipoSolicitante is null || !TipoCoherente(usuario.TipoUsuario, modelo.TipoSolicitante.Value))
        {
            return ResultadoRegistroSolicitud.Fallo(ErrorRegistroSolicitud.TipoSolicitanteInvalido,
                "El tipo de solicitante no corresponde al tipo de cuenta.");
        }

        var impedimento = await EvaluarAsync(usuario, modelo.NroEquipo);

        if (impedimento is not null)
        {
            return ResultadoRegistroSolicitud.Fallo(impedimento.Error, impedimento.Mensaje);
        }

        var solicitud = new Solicitud
        {
            NroUsuario = usuario.NroUsuario,
            TipoSolicitante = modelo.TipoSolicitante.Value,
            NroEquipoRequerido = modelo.NroEquipo,
            MotivoSolicitud = modelo.MotivoSolicitud.Trim(),
            OrigenSolicitud = OrigenSolicitud.Autoservicio,
            FechaSolicitud = DateTime.UtcNow,
            EstadoSolicitud = EstadoSolicitud.Pendiente
        };

        _context.Solicitudes.Add(solicitud);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
        {
            return ResultadoRegistroSolicitud.Fallo(ErrorRegistroSolicitud.SolicitudDuplicada, MensajeSolicitudDuplicada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el registro de la solicitud del Usuario {NroUsuario} para el Equipo {NroEquipo}",
                usuario.NroUsuario, modelo.NroEquipo);
            return ResultadoRegistroSolicitud.Fallo(ErrorRegistroSolicitud.Fallido,
                "No se pudo registrar la solicitud. Intentar nuevamente.");
        }

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.Solicitud, null, nameof(Solicitud), solicitud.NroSolicitud,
                "Registro de solicitud");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar el registro de la Solicitud {NroSolicitud}", solicitud.NroSolicitud);
        }

        return ResultadoRegistroSolicitud.Ok(solicitud.NroSolicitud);
    }

    public async Task<IdentityResult> EditarMotivoAsync(string identityUserId, int nroSolicitud, string motivo)
    {
        var solicitud = await _context.Solicitudes
            .FirstOrDefaultAsync(s => s.NroSolicitud == nroSolicitud
                && s.Usuario.IdentityUserId == identityUserId
                && s.Usuario.EstadoUsuario == EstadoUsuario.Activo);

        if (solicitud is null)
        {
            return Fallo("SolicitudNoEncontrada", "No se encontró la solicitud.");
        }

        if (solicitud.EstadoSolicitud != EstadoSolicitud.Pendiente)
        {
            return Fallo("SolicitudNoEditable",
                "La solicitud ya no puede editarse porque dejó de estar Pendiente.");
        }

        var motivoNuevo = motivo.Trim();

        if (motivoNuevo == solicitud.MotivoSolicitud)
        {
            return IdentityResult.Success;
        }

        solicitud.MotivoSolicitud = motivoNuevo;
        solicitud.FechaUltimaModificacion = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fallo("SolicitudModificada",
                "La solicitud cambió mientras se editaba. Volver a abrirla e intentar nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la edición del motivo de la Solicitud {NroSolicitud}", nroSolicitud);
            return Fallo("EdicionFallida", "No se pudo guardar el cambio. Intentar nuevamente.");
        }

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.Solicitud, null, nameof(Solicitud), solicitud.NroSolicitud,
                "Edición del motivo de la solicitud");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar la edición de la Solicitud {NroSolicitud}", nroSolicitud);
        }

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> CancelarAsync(string identityUserId, int nroSolicitud)
    {
        var solicitud = await _context.Solicitudes
            .FirstOrDefaultAsync(s => s.NroSolicitud == nroSolicitud
                && s.Usuario.IdentityUserId == identityUserId
                && s.Usuario.EstadoUsuario == EstadoUsuario.Activo);

        if (solicitud is null)
        {
            return Fallo("SolicitudNoEncontrada", "No se encontró la solicitud.");
        }

        if (solicitud.EstadoSolicitud != EstadoSolicitud.Pendiente)
        {
            return Fallo("SolicitudNoCancelable",
                "La solicitud ya no puede cancelarse porque fue asignada o cambió de estado.");
        }

        var ahora = DateTime.UtcNow;

        solicitud.EstadoSolicitud = EstadoSolicitud.Cancelada;
        solicitud.FechaCancelacion = ahora;
        solicitud.FechaCierre = ahora;
        solicitud.MotivoCancelacion = MotivoCancelacionSolicitante;
        solicitud.FechaUltimaModificacion = ahora;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fallo("SolicitudModificada",
                "La solicitud cambió mientras se cancelaba. Volver a abrirla e intentar nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la cancelación de la Solicitud {NroSolicitud}", nroSolicitud);
            return Fallo("CancelacionFallida", "No se pudo cancelar la solicitud. Intentar nuevamente.");
        }

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.Solicitud, null, nameof(Solicitud), solicitud.NroSolicitud,
                "Cancelación de solicitud");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar la cancelación de la Solicitud {NroSolicitud}", nroSolicitud);
        }

        return IdentityResult.Success;
    }
}