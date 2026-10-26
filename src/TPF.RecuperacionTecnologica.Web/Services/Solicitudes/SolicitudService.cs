using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.Services.Configuracion;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Usuarios;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

namespace TPF.RecuperacionTecnologica.Web.Services.Solicitudes;

public class SolicitudService : ISolicitudService
{
    // se usa solo si todavía no existe la fila de configuración
    private const int MaximoPendientesPorDefecto = 3;

    private const string MensajeUsuarioNoHabilitado = "La cuenta no está habilitada para registrar solicitudes.";
    private const string MensajeSolicitudDuplicada = "Esta cuenta ya tiene una solicitud pendiente para este equipo. Su estado se puede ver en Mis solicitudes.";
    private const string MotivoCancelacionSolicitante = "Cancelada por la persona solicitante.";

    private static IdentityResult Fallo(string codigo, string descripcion) =>
        IdentityResult.Failed(new IdentityError { Code = codigo, Description = descripcion });

    private readonly ApplicationDbContext _context;
    private readonly IUsuarioService _usuarioService;
    private readonly ICuentaService _cuentaService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IConfiguracionService _configuracionService;
    private readonly ILogger<SolicitudService> _logger;

    public SolicitudService(
        ApplicationDbContext context,
        IUsuarioService usuarioService,
        ICuentaService cuentaService,
        IAuditoriaService auditoriaService,
        IConfiguracionService configuracionService,
        ILogger<SolicitudService> logger)
    {
        _context = context;
        _usuarioService = usuarioService;
        _cuentaService = cuentaService;
        _auditoriaService = auditoriaService;
        _configuracionService = configuracionService;
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

    private async Task<ImpedimentoSolicitud?> EvaluarAsync(Usuario? usuario, int nroEquipo)
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

        if (usuario is null)
        {
            return null;
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

        var maximoPendientes = (await _configuracionService.ObtenerAsync())?.MaximoSolicitudesPendientes
            ?? MaximoPendientesPorDefecto;

        if (equiposPendientes.Count >= maximoPendientes)
        {
            return new ImpedimentoSolicitud(ErrorRegistroSolicitud.LimiteAlcanzado,
                $"Esta cuenta ya tiene {maximoPendientes} solicitudes pendientes, que es el máximo. Hay que esperar a que alguna se resuelva o cancelarla para pedir otro equipo.");
        }

        return null;
    }

    public async Task<ImpedimentoSolicitud?> ObtenerImpedimentoParaUsuarioAsync(int nroUsuario, int nroEquipo)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NroUsuario == nroUsuario);

        if (usuario is null || usuario.EstadoUsuario != EstadoUsuario.Activo)
        {
            return new ImpedimentoSolicitud(ErrorRegistroSolicitud.UsuarioNoHabilitado,
                "El usuario no existe o no está activo, por eso no se puede registrar una solicitud a su nombre.");
        }

        return await EvaluarAsync(usuario, nroEquipo);
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

    public async Task<ResultadoRegistroSolicitud> RegistrarAsistidaAsync(
        string identityUserIdCoordinador, RegistroSolicitudAsistidaViewModel modelo,
        Func<string, string, string> construirRutaInvitacion, string urlBaseSolicitud)
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
            return ResultadoRegistroSolicitud.Fallo(ErrorRegistroSolicitud.CoordinadorNoHabilitado,
                "La cuenta no está habilitada para registrar solicitudes asistidas.");
        }

        if (modelo.TipoSolicitante is null || modelo.NroEquipo is null)
        {
            return ResultadoRegistroSolicitud.Fallo(ErrorRegistroSolicitud.Fallido, "Faltan datos de la solicitud.");
        }

        return modelo.Usuario.Modo == ModoUsuarioAsistido.Existente
            ? await RegistrarAsistidaParaExistenteAsync(codigoCoordinador.Value, modelo)
            : await RegistrarAsistidaParaNuevoAsync(codigoCoordinador.Value, modelo, construirRutaInvitacion, urlBaseSolicitud);
    }

    private static Solicitud NuevaSolicitudAsistida(int nroUsuario, RegistroSolicitudAsistidaViewModel modelo, int codigoCoordinador) => new()
    {
        NroUsuario = nroUsuario,
        TipoSolicitante = modelo.TipoSolicitante!.Value,
        NroEquipoRequerido = modelo.NroEquipo!.Value,
        MotivoSolicitud = modelo.MotivoSolicitud.Trim(),
        OrigenSolicitud = OrigenSolicitud.Asistida,
        CodigoCoordinador = codigoCoordinador,
        FechaSolicitud = DateTime.UtcNow,
        EstadoSolicitud = EstadoSolicitud.Pendiente
    };

    private async Task AuditarAsync(TipoOperacionAuditoria tipo, int codigoPersonal, string entidad, int id, string detalle)
    {
        try
        {
            await _auditoriaService.RegistrarEventoAsync(tipo, codigoPersonal, entidad, id, detalle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar el evento {Tipo} de la entidad {Entidad} {Id}", tipo, entidad, id);
        }
    }

    private async Task<ResultadoRegistroSolicitud> RegistrarAsistidaParaExistenteAsync(
        int codigoCoordinador, RegistroSolicitudAsistidaViewModel modelo)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NroUsuario == modelo.Usuario.NroUsuario);

        if (usuario is null || usuario.EstadoUsuario != EstadoUsuario.Activo)
        {
            return ResultadoRegistroSolicitud.Fallo(ErrorRegistroSolicitud.UsuarioNoHabilitado,
                "El usuario no existe o no está activo, por eso no se puede registrar una solicitud a su nombre.");
        }

        var impedimento = await EvaluarAsync(usuario, modelo.NroEquipo!.Value);

        if (impedimento is not null)
        {
            return ResultadoRegistroSolicitud.Fallo(impedimento.Error, impedimento.Mensaje);
        }

        var solicitud = NuevaSolicitudAsistida(usuario.NroUsuario, modelo, codigoCoordinador);
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
            _logger.LogError(ex, "Falló el registro asistido de la solicitud del Usuario {NroUsuario}", usuario.NroUsuario);
            return ResultadoRegistroSolicitud.Fallo(ErrorRegistroSolicitud.Fallido,
                "No se pudo registrar la solicitud. Intentar nuevamente.");
        }

        await AuditarAsync(TipoOperacionAuditoria.Solicitud, codigoCoordinador, nameof(Solicitud),
            solicitud.NroSolicitud, "Registro de solicitud asistida");

        return ResultadoRegistroSolicitud.Ok(solicitud.NroSolicitud);
    }

    private async Task<ResultadoRegistroSolicitud> RegistrarAsistidaParaNuevoAsync(
        int codigoCoordinador, RegistroSolicitudAsistidaViewModel modelo,
        Func<string, string, string> construirRutaInvitacion, string urlBaseSolicitud)
    {
        var impedimento = await EvaluarAsync(null, modelo.NroEquipo!.Value);

        if (impedimento is not null)
        {
            return ResultadoRegistroSolicitud.Fallo(impedimento.Error, impedimento.Mensaje);
        }

        string identityUserId;
        Usuario usuario;
        Solicitud solicitud;

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            var alta = await _usuarioService.AltaAsistidaAsync(modelo.Usuario);

            if (!alta.Exitoso)
            {
                return alta.YaExiste
                    ? ResultadoRegistroSolicitud.Fallo(ErrorRegistroSolicitud.UsuarioYaExiste, alta.Mensaje!)
                    : ResultadoRegistroSolicitud.Fallo(ErrorRegistroSolicitud.Fallido, alta.Mensaje ?? "No se pudo crear la cuenta de la persona.");
            }

            usuario = alta.Usuario!;
            identityUserId = alta.IdentityUserId!;

            solicitud = NuevaSolicitudAsistida(usuario.NroUsuario, modelo, codigoCoordinador);
            _context.Solicitudes.Add(solicitud);
            await _context.SaveChangesAsync();

            await transaccion.CommitAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
        {
            return ResultadoRegistroSolicitud.Fallo(ErrorRegistroSolicitud.UsuarioYaExiste, ResultadoAltaAsistida.MensajeYaExiste);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el alta asistida del usuario y su solicitud; se revirtió la transacción");
            return ResultadoRegistroSolicitud.Fallo(ErrorRegistroSolicitud.Fallido,
                "No se pudo completar el registro. No se guardó nada. Intentar nuevamente.");
        }

        await AuditarAsync(TipoOperacionAuditoria.AltaUsuario, codigoCoordinador, nameof(Usuario),
            usuario.NroUsuario, $"Alta asistida de usuario ({usuario.TipoUsuario})");
        await AuditarAsync(TipoOperacionAuditoria.Solicitud, codigoCoordinador, nameof(Solicitud),
            solicitud.NroSolicitud, "Registro de solicitud asistida");

        try
        {
            await _cuentaService.EnviarInvitacionAsync(identityUserId, construirRutaInvitacion, urlBaseSolicitud);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar la invitación al Usuario {NroUsuario}", usuario.NroUsuario);
        }

        return ResultadoRegistroSolicitud.Ok(solicitud.NroSolicitud);
    }
}