using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Home;
using TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

namespace TPF.RecuperacionTecnologica.Web.Services.Home;

public class InicioService : IInicioService
{
    private const int CantidadReciente = 4;
    private const int CantidadActividad = 5;

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
