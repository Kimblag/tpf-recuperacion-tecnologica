using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;
using TPF.RecuperacionTecnologica.Web.ViewModels.Home;
using TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

namespace TPF.RecuperacionTecnologica.Web.Services.Home;

public class InicioService : IInicioService
{
    private const int CantidadReciente = 4;
    private const int CantidadActividad = 5;
    private const int CantidadPendientes = 5;
    private const int DiasAvisoVencimiento = 3;

    private readonly ApplicationDbContext _context;

    public InicioService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InicioUsuarioViewModel?> ObtenerInicioUsuarioAsync(string identityUserId)
    {
        var nroUsuario = await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.IdentityUserId == identityUserId)
            .Select(u => (int?)u.NroUsuario)
            .FirstOrDefaultAsync();

        if (nroUsuario is null)
        {
            return null;
        }

        var donaciones = _context.Equipos
            .AsNoTracking()
            .Where(e => e.NroUsuarioDonante == nroUsuario);

        var solicitudes = _context.Solicitudes
            .AsNoTracking()
            .Where(s => s.NroUsuario == nroUsuario);

        EstadoEquipo[] estadosEnProceso =
        [
            EstadoEquipo.PendienteRecepcion,
            EstadoEquipo.EnReparacion,
            EstadoEquipo.Disponible,
            EstadoEquipo.Asignado
        ];

        var donacionesActivas = await donaciones.CountAsync(e => estadosEnProceso.Contains(e.EstadoEquipo));
        var pendientesDeRecepcion = await donaciones.CountAsync(e => e.EstadoEquipo == EstadoEquipo.PendienteRecepcion);
        var solicitudesPendientes = await solicitudes.CountAsync(s => s.EstadoSolicitud == EstadoSolicitud.Pendiente);
        var equiposDisponibles = await _context.Equipos.CountAsync(e => e.EstadoEquipo == EstadoEquipo.Disponible);
        var entregasRecibidas = await _context.Entregas.CountAsync(e => e.Solicitud.NroUsuario == nroUsuario);

        var donacionesRecientes = await donaciones
            .OrderByDescending(e => e.FechaRegistro)
            .ThenByDescending(e => e.NroEquipo)
            .Take(CantidadReciente)
            .Select(e => new DonacionFilaViewModel
            {
                NroEquipo = e.NroEquipo,
                TipoEquipo = e.TipoEquipo,
                Marca = e.Marca,
                Modelo = e.Modelo,
                EstadoInicialDeclarado = e.EstadoInicialDeclarado,
                FechaRegistro = e.FechaRegistro,
                EstadoEquipo = e.EstadoEquipo
            })
            .ToListAsync();

        var solicitudesRecientes = await solicitudes
            .OrderByDescending(s => s.FechaSolicitud)
            .ThenByDescending(s => s.NroSolicitud)
            .Take(CantidadReciente)
            .Select(s => new SolicitudFilaViewModel
            {
                NroSolicitud = s.NroSolicitud,
                NroEquipo = s.NroEquipoRequerido,
                TipoSolicitante = s.TipoSolicitante,
                OrigenSolicitud = s.OrigenSolicitud,
                FechaSolicitud = s.FechaSolicitud,
                EstadoSolicitud = s.EstadoSolicitud
            })
            .ToListAsync();

        return new InicioUsuarioViewModel
        {
            DonacionesActivas = donacionesActivas,
            DonacionesPendientesDeRecepcion = pendientesDeRecepcion,
            SolicitudesPendientes = solicitudesPendientes,
            EquiposDisponibles = equiposDisponibles,
            EntregasRecibidas = entregasRecibidas,
            DonacionesRecientes = donacionesRecientes,
            SolicitudesRecientes = solicitudesRecientes,
            Actividad = await ObtenerActividadAsync(nroUsuario.Value)
        };
    }

    private async Task<IReadOnlyList<ActividadViewModel>> ObtenerActividadAsync(int nroUsuario)
    {
        var eventos = new List<(DateTime Fecha, ActividadViewModel Actividad)>();

        var registros = await _context.Equipos
            .AsNoTracking()
            .Where(e => e.NroUsuarioDonante == nroUsuario)
            .OrderByDescending(e => e.FechaRegistro)
            .Take(CantidadActividad)
            .Select(e => new { e.NroEquipo, e.FechaRegistro })
            .ToListAsync();

        eventos.AddRange(registros.Select(r => (r.FechaRegistro,
            Evento("gift", "Donación del equipo ", CodigoEquipo(r.NroEquipo), " registrada", r.FechaRegistro, exito: true))));

        var cancelaciones = await _context.Equipos
            .AsNoTracking()
            .Where(e => e.NroUsuarioDonante == nroUsuario && e.FechaCancelacion != null)
            .OrderByDescending(e => e.FechaCancelacion)
            .Take(CantidadActividad)
            .Select(e => new { e.NroEquipo, Fecha = e.FechaCancelacion!.Value })
            .ToListAsync();

        eventos.AddRange(cancelaciones.Select(c => (c.Fecha,
            Evento("x", "Donación del equipo ", CodigoEquipo(c.NroEquipo), " cancelada", c.Fecha))));

        var diagnosticos = await _context.Diagnosticos
            .AsNoTracking()
            .Where(d => d.Equipo.NroUsuarioDonante == nroUsuario)
            .OrderByDescending(d => d.FechaDiagnostico)
            .Take(CantidadActividad)
            .Select(d => new { d.NroEquipo, d.FechaDiagnostico, d.DictamenTecnico })
            .ToListAsync();

        eventos.AddRange(diagnosticos.Select(d => (d.FechaDiagnostico,
            Evento("wrench", "Diagnóstico del equipo ", CodigoEquipo(d.NroEquipo), " completado", d.FechaDiagnostico,
                $"Dictamen: {TextoDictamen(d.DictamenTecnico)}"))));

        var asignaciones = await _context.Asignaciones
            .AsNoTracking()
            .Where(a => a.Equipo.NroUsuarioDonante == nroUsuario && a.EstadoAsignacion != EstadoAsignacion.Liberada)
            .OrderByDescending(a => a.FechaAsignacion)
            .Take(CantidadActividad)
            .Select(a => new { a.NroEquipo, a.FechaAsignacion })
            .ToListAsync();

        eventos.AddRange(asignaciones.Select(a => (a.FechaAsignacion,
            Evento("swap", "Equipo ", CodigoEquipo(a.NroEquipo), " asignado a una solicitud", a.FechaAsignacion))));

        var entregas = await _context.Entregas
            .AsNoTracking()
            .Where(e => e.Equipo.NroUsuarioDonante == nroUsuario)
            .OrderByDescending(e => e.FechaEntrega)
            .Take(CantidadActividad)
            .Select(e => new { e.NroEquipo, e.FechaEntrega })
            .ToListAsync();

        eventos.AddRange(entregas.Select(e => (e.FechaEntrega,
            Evento("truck", "Equipo ", CodigoEquipo(e.NroEquipo), " entregado", e.FechaEntrega))));

        var solicitudes = await _context.Solicitudes
            .AsNoTracking()
            .Where(s => s.NroUsuario == nroUsuario)
            .OrderByDescending(s => s.FechaSolicitud)
            .Take(CantidadActividad)
            .Select(s => new { s.NroSolicitud, s.FechaSolicitud, s.OrigenSolicitud })
            .ToListAsync();

        eventos.AddRange(solicitudes.Select(s => (s.FechaSolicitud,
            Evento("tray", "Solicitud ", $"SOL-{s.NroSolicitud:D6}", " registrada", s.FechaSolicitud,
                s.OrigenSolicitud == OrigenSolicitud.Asistida ? "Asistida" : "Autoservicio", exito: true))));

        return eventos
            .OrderByDescending(e => e.Fecha)
            .Take(CantidadActividad)
            .Select(e => e.Actividad)
            .ToList();
    }

    public async Task<TecnicoInicioViewModel?> ObtenerInicioTecnicoAsync(string identityUserId)
    {
        var codigoPersonal = await _context.PersonalInterno
            .AsNoTracking()
            .Where(p => p.IdentityUserId == identityUserId)
            .Select(p => (int?)p.CodigoPersonal)
            .FirstOrDefaultAsync();

        if (codigoPersonal is null)
        {
            return null;
        }

        var equipos = _context.Equipos.AsNoTracking();
        var enCola = equipos.Where(e => e.EstadoEquipo == EstadoEquipo.PendienteRecepcion || e.EstadoEquipo == EstadoEquipo.EnReparacion);

        var pendientesDeDiagnostico = await equipos.CountAsync(e => e.EstadoEquipo == EstadoEquipo.PendienteRecepcion);
        var fechaMasAntiguo = await equipos
            .Where(e => e.EstadoEquipo == EstadoEquipo.PendienteRecepcion)
            .Select(e => (DateTime?)e.FechaRegistro)
            .MinAsync();
        var enReparacion = await equipos.CountAsync(e => e.EstadoEquipo == EstadoEquipo.EnReparacion);
        var disponibles = await equipos.CountAsync(e => e.EstadoEquipo == EstadoEquipo.Disponible);

        var ahora = DateTime.UtcNow;
        var inicioDeMes = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var propios = _context.Diagnosticos
            .AsNoTracking()
            .Where(d => d.CodigoTecnico == codigoPersonal);

        var diagnosticosDelMes = await propios.CountAsync(d => d.FechaDiagnostico >= inicioDeMes);

        var pendientes = await enCola
            .OrderBy(e => e.FechaRegistro)
            .ThenBy(e => e.NroEquipo)
            .Take(CantidadPendientes)
            .Select(e => new EquipoFilaViewModel
            {
                NroEquipo = e.NroEquipo,
                TipoEquipo = e.TipoEquipo,
                Marca = e.Marca,
                Modelo = e.Modelo,
                EstadoInicialDeclarado = e.EstadoInicialDeclarado,
                FechaRegistro = e.FechaRegistro,
                EstadoEquipo = e.EstadoEquipo
            })
            .ToListAsync();

        var recientes = await propios
            .OrderByDescending(d => d.FechaDiagnostico)
            .ThenByDescending(d => d.NroDiagnostico)
            .Take(CantidadActividad)
            .Select(d => new { d.NroDiagnostico, d.NroEquipo, d.TipoDiagnostico, d.FechaDiagnostico, d.DictamenTecnico })
            .ToListAsync();

        var actividad = recientes
            .Select(d => d.TipoDiagnostico switch
            {
                TipoDiagnostico.Rectificacion => Evento("edit", "Rectificación ", $"DIA-{d.NroDiagnostico:D6}", " guardada",
                    d.FechaDiagnostico, $"Equipo {CodigoEquipo(d.NroEquipo)} · {TextoDictamen(d.DictamenTecnico)}"),
                TipoDiagnostico.Segundo => Evento("wrench", "Segundo diagnóstico ", $"DIA-{d.NroDiagnostico:D6}", " registrado",
                    d.FechaDiagnostico, $"Equipo {CodigoEquipo(d.NroEquipo)} · {TextoDictamen(d.DictamenTecnico)}",
                    exito: d.DictamenTecnico == DictamenTecnico.Funcional),
                _ => Evento("wrench", "Diagnóstico ", $"DIA-{d.NroDiagnostico:D6}", " registrado",
                    d.FechaDiagnostico, $"Equipo {CodigoEquipo(d.NroEquipo)} · {TextoDictamen(d.DictamenTecnico)}",
                    exito: d.DictamenTecnico == DictamenTecnico.Funcional)
            })
            .ToList();

        return new TecnicoInicioViewModel
        {
            PendientesDeDiagnostico = pendientesDeDiagnostico,
            FechaPendienteMasAntiguo = fechaMasAntiguo,
            EnReparacion = enReparacion,
            DiagnosticosDelMes = diagnosticosDelMes,
            EquiposDisponibles = disponibles,
            Pendientes = pendientes,
            ActividadReciente = actividad
        };
    }

    public async Task<CoordinadorInicioViewModel?> ObtenerInicioCoordinadorAsync(string identityUserId)
    {
        var esCoordinador = await _context.PersonalInterno
            .AsNoTracking()
            .AnyAsync(p => p.IdentityUserId == identityUserId
                && p.RolAsignado == RolPersonal.Coordinador
                && p.EstadoPersonal == EstadoPersonal.Activo);

        if (!esCoordinador)
        {
            return null;
        }

        var ahora = DateTime.UtcNow;
        var hoy = ahora.Date;
        var limiteAviso = hoy.AddDays(DiasAvisoVencimiento);
        var haceUnaSemana = ahora.AddDays(-7);
        var inicioDeMes = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var inicioDeMesAnterior = inicioDeMes.AddMonths(-1);

        var disponibles = _context.Equipos
            .AsNoTracking()
            .Where(e => e.EstadoEquipo == EstadoEquipo.Disponible);

        var equiposDisponibles = await disponibles.CountAsync();
        var disponiblesEstaSemana = await disponibles.CountAsync(e => e.FechaDisponibilidad >= haceUnaSemana);

        var pendientes = _context.Solicitudes
            .AsNoTracking()
            .Where(s => s.EstadoSolicitud == EstadoSolicitud.Pendiente);

        var solicitudesPendientes = await pendientes.CountAsync();
        var pendientesInstitucionales = await pendientes.CountAsync(s => s.TipoSolicitante != TipoSolicitante.Particular);

        var vigentes = _context.Asignaciones
            .AsNoTracking()
            .Where(a => a.EstadoAsignacion == EstadoAsignacion.Vigente);

        var asignacionesVigentes = await vigentes.CountAsync();
        var vencidas = await vigentes.CountAsync(a => a.FechaLimiteRetiro.Date < hoy);
        var porVencer = await vigentes.CountAsync(a => a.FechaLimiteRetiro.Date >= hoy && a.FechaLimiteRetiro.Date <= limiteAviso);

        var entregas = _context.Entregas.AsNoTracking();
        var entregasDelMes = await entregas.CountAsync(e => e.FechaEntrega >= inicioDeMes);
        var entregasDelMesAnterior = await entregas
            .CountAsync(e => e.FechaEntrega >= inicioDeMesAnterior && e.FechaEntrega < inicioDeMes);

        var cola = await pendientes
            .OrderBy(s => s.TipoSolicitante == TipoSolicitante.Particular ? 1 : 0)
            .ThenBy(s => s.FechaSolicitud)
            .ThenBy(s => s.NroSolicitud)
            .Take(CantidadPendientes)
            .Select(s => new
            {
                s.NroSolicitud,
                s.TipoSolicitante,
                s.FechaSolicitud,
                s.NroEquipoRequerido,
                s.EquipoRequerido.TipoEquipo,
                s.EquipoRequerido.Marca,
                s.EquipoRequerido.Modelo,
                s.Usuario.RazonSocial,
                s.Usuario.Nombre,
                s.Usuario.Apellido
            })
            .ToListAsync();

        var conPlazo = await vigentes
            .OrderBy(a => a.FechaLimiteRetiro)
            .ThenBy(a => a.NroAsignacion)
            .Take(CantidadPendientes)
            .Select(a => new
            {
                a.NroAsignacion,
                a.FechaLimiteRetiro,
                a.Equipo.Marca,
                a.Equipo.Modelo,
                a.Solicitud.Usuario.RazonSocial,
                a.Solicitud.Usuario.Nombre,
                a.Solicitud.Usuario.Apellido
            })
            .ToListAsync();

        var tiposDeActividad = new[]
        {
            TipoOperacionAuditoria.Donacion,
            TipoOperacionAuditoria.Solicitud,
            TipoOperacionAuditoria.Asignacion,
            TipoOperacionAuditoria.Liberacion,
            TipoOperacionAuditoria.Entrega
        };

        var registros = await _context.RegistrosAuditoria
            .AsNoTracking()
            .Where(r => tiposDeActividad.Contains(r.TipoOperacion))
            .OrderByDescending(r => r.FechaHora)
            .ThenByDescending(r => r.NroAuditoria)
            .Take(CantidadActividad)
            .Select(r => new { r.FechaHora, r.TipoOperacion, r.EntidadAfectada, r.IdEntidadAfectada, r.Detalle })
            .ToListAsync();

        return new CoordinadorInicioViewModel
        {
            EquiposDisponibles = equiposDisponibles,
            DisponiblesEstaSemana = disponiblesEstaSemana,
            SolicitudesPendientes = solicitudesPendientes,
            PendientesInstitucionales = pendientesInstitucionales,
            AsignacionesVigentes = asignacionesVigentes,
            AsignacionesPorVencer = porVencer,
            AsignacionesVencidas = vencidas,
            EntregasDelMes = entregasDelMes,
            EntregasDelMesAnterior = entregasDelMesAnterior,
            ColaPrioritaria = cola.Select(s => new SolicitudPrioritariaViewModel
            {
                NroSolicitud = s.NroSolicitud,
                Solicitante = NombreDe(s.RazonSocial, s.Nombre, s.Apellido),
                TipoSolicitante = s.TipoSolicitante,
                TipoEquipo = s.TipoEquipo,
                NroEquipo = s.NroEquipoRequerido,
                MarcaYModelo = $"{s.Marca} {s.Modelo}",
                FechaSolicitud = s.FechaSolicitud
            }).ToList(),
            AsignacionesConPlazo = conPlazo.Select(a => new AsignacionConPlazoViewModel
            {
                NroAsignacion = a.NroAsignacion,
                Solicitante = NombreDe(a.RazonSocial, a.Nombre, a.Apellido),
                MarcaYModelo = $"{a.Marca} {a.Modelo}",
                FechaLimiteRetiro = a.FechaLimiteRetiro,
                DiasRestantes = (a.FechaLimiteRetiro.Date - hoy).Days
            }).ToList(),
            ActividadReciente = registros.Select(r => EventoDeAuditoria(
                r.TipoOperacion, r.EntidadAfectada, r.IdEntidadAfectada, r.Detalle, r.FechaHora)).ToList()
        };
    }

    private static string NombreDe(string? razonSocial, string? nombre, string? apellido) =>
        !string.IsNullOrWhiteSpace(razonSocial) ? razonSocial : $"{nombre} {apellido}".Trim();

    private static ActividadViewModel EventoDeAuditoria(
        TipoOperacionAuditoria tipo, string entidad, int id, string? detalle, DateTime fecha)
    {
        var icono = tipo switch
        {
            TipoOperacionAuditoria.Donacion => "gift",
            TipoOperacionAuditoria.Solicitud => "tray",
            TipoOperacionAuditoria.Asignacion => "swap",
            TipoOperacionAuditoria.Liberacion => "undo",
            _ => "truck"
        };

        var codigo = entidad switch
        {
            "Solicitud" => $"SOL-{id:D6}",
            "Asignacion" => $"ASG-{id:D6}",
            "Entrega" => $"ENT-{id:D6}",
            _ => id.ToString("D6")
        };

        return Evento(icono, $"{detalle ?? "Movimiento registrado"} ", codigo, "", fecha,
            exito: tipo != TipoOperacionAuditoria.Liberacion);
    }

    private static string CodigoEquipo(int nroEquipo) => nroEquipo.ToString("D6");

    private static string TextoDictamen(DictamenTecnico dictamen) => dictamen switch
    {
        DictamenTecnico.NecesitaReparacion => "Necesita reparación",
        DictamenTecnico.NoRecuperable => "No recuperable",
        _ => "Funcional"
    };

    private static ActividadViewModel Evento(
        string icono, string textoAntes, string codigo, string textoDespues, DateTime fecha,
        string? detalle = null, bool exito = false) => new()
        {
            Icono = icono,
            TextoAntes = textoAntes,
            Codigo = codigo,
            TextoDespues = textoDespues,
            Detalle = detalle is null ? fecha.ToString("dd/MM/yyyy") : $"{fecha:dd/MM/yyyy} · {detalle}",
            EsExito = exito
        };
}
