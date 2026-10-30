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
    private const int MaximoMotivoLiberacion = 300;

    private const string MensajeConcurrenciaLiberacion =
        "La asignación cambió mientras se liberaba: otra persona pudo entregarla o liberarla. No se guardó nada. Volver a abrir el detalle e intentar nuevamente.";

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

    // se usa solo si todavía no existe la fila de configuración
    private const int DiasAvisoVencimientoPorDefecto = 3;

    private async Task<int> ObtenerDiasAvisoAsync() =>
        (await _configuracionService.ObtenerAsync())?.DiasAvisoVencimiento ?? DiasAvisoVencimientoPorDefecto;

    private static IQueryable<Asignacion> FiltrarPorTexto(IQueryable<Asignacion> consulta, string? busqueda)
    {
        var texto = busqueda?.Trim();

        if (string.IsNullOrEmpty(texto))
        {
            return consulta;
        }

        return consulta.Where(a =>
            ("ASG-" + (1000000 + a.NroAsignacion).ToString().Substring(1)).Contains(texto)
            || ("SOL-" + (1000000 + a.NroSolicitud).ToString().Substring(1)).Contains(texto)
            || (1000000 + a.NroEquipo).ToString().Substring(1).Contains(texto)
            || (a.Equipo.Marca + " " + a.Equipo.Modelo).Contains(texto)
            || (a.Solicitud.Usuario.RazonSocial ?? (a.Solicitud.Usuario.Nombre + " " + a.Solicitud.Usuario.Apellido)).Contains(texto));
    }

    public async Task<ResultadoPaginado<AsignacionFilaViewModel>> ListarAsync(
        string? busqueda, EstadoAsignacion? estado, FiltroPlazoAsignacion? plazo, int pagina, int tamanoPagina)
    {
        // "hoy" es el día UTC, igual que en el inicio del Coordinador
        var hoy = DateTime.UtcNow.Date;
        var limiteAviso = hoy.AddDays(await ObtenerDiasAvisoAsync());

        var consulta = _context.Asignaciones.AsNoTracking().AsQueryable();

        if (plazo is not null)
        {
            // el plazo solo tiene sentido en las Vigentes, sin importar el estado elegido
            consulta = consulta.Where(a => a.EstadoAsignacion == EstadoAsignacion.Vigente);

            consulta = plazo == FiltroPlazoAsignacion.Vencidas
                ? consulta.Where(a => a.FechaLimiteRetiro.Date < hoy)
                : consulta.Where(a => a.FechaLimiteRetiro.Date >= hoy && a.FechaLimiteRetiro.Date <= limiteAviso);
        }
        else if (estado is not null)
        {
            consulta = consulta.Where(a => a.EstadoAsignacion == estado);
        }

        consulta = FiltrarPorTexto(consulta, busqueda);

        var total = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var filas = await consulta
            .OrderByDescending(a => a.FechaAsignacion)
            .ThenByDescending(a => a.NroAsignacion)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(a => new
            {
                a.NroAsignacion,
                a.NroSolicitud,
                a.NroEquipo,
                a.Equipo.TipoEquipo,
                a.Equipo.Marca,
                a.Equipo.Modelo,
                CoordinadorNombre = a.Coordinador.Nombre,
                CoordinadorApellido = a.Coordinador.Apellido,
                a.FechaAsignacion,
                a.FechaLimiteRetiro,
                a.EstadoAsignacion,
                a.Solicitud.Usuario.RazonSocial,
                a.Solicitud.Usuario.Nombre,
                a.Solicitud.Usuario.Apellido
            })
            .ToListAsync();

        return new ResultadoPaginado<AsignacionFilaViewModel>
        {
            Elementos = filas.Select(f =>
            {
                var vigente = f.EstadoAsignacion == EstadoAsignacion.Vigente;

                return new AsignacionFilaViewModel
                {
                    NroAsignacion = f.NroAsignacion,
                    NroSolicitud = f.NroSolicitud,
                    NroEquipo = f.NroEquipo,
                    TipoEquipo = f.TipoEquipo,
                    MarcaYModelo = $"{f.Marca} {f.Modelo}",
                    Coordinador = $"{f.CoordinadorNombre} {f.CoordinadorApellido}",
                    FechaAsignacion = f.FechaAsignacion,
                    FechaLimiteRetiro = f.FechaLimiteRetiro,
                    EstadoAsignacion = f.EstadoAsignacion,
                    Solicitante = NombreSolicitante(f.RazonSocial, f.Nombre, f.Apellido),
                    PlazoVencido = vigente && f.FechaLimiteRetiro.Date < hoy,
                    PlazoPorVencer = vigente && f.FechaLimiteRetiro.Date >= hoy && f.FechaLimiteRetiro.Date <= limiteAviso
                };
            }).ToList(),
            Total = total,
            Pagina = pagina
        };
    }

    public async Task<AsignacionDetalleViewModel?> ObtenerDetalleAsync(int nroAsignacion)
    {
        var datos = await _context.Asignaciones
            .AsNoTracking()
            .Where(a => a.NroAsignacion == nroAsignacion)
            .Select(a => new
            {
                a.NroAsignacion,
                a.NroSolicitud,
                a.NroEquipo,
                a.Equipo.TipoEquipo,
                a.Equipo.Marca,
                a.Equipo.Modelo,
                a.Solicitud.TipoSolicitante,
                a.Solicitud.Usuario.RazonSocial,
                a.Solicitud.Usuario.Nombre,
                a.Solicitud.Usuario.Apellido,
                CoordinadorNombre = a.Coordinador.Nombre,
                CoordinadorApellido = a.Coordinador.Apellido,
                a.CodigoCoordinador,
                a.FechaAsignacion,
                a.FechaLimiteRetiro,
                a.EstadoAsignacion,
                a.JustificacionCambio,
                a.FechaLiberacion,
                a.MotivoLiberacion
            })
            .FirstOrDefaultAsync();

        if (datos is null)
        {
            return null;
        }

        var fechaEntrega = await _context.Entregas
            .AsNoTracking()
            .Where(e => e.NroAsignacion == nroAsignacion)
            .Select(e => (DateTime?)e.FechaEntrega)
            .FirstOrDefaultAsync();

        var coordinador = $"{datos.CoordinadorNombre} {datos.CoordinadorApellido} · PER-{datos.CodigoCoordinador:D4}";
        var vencido = datos.EstadoAsignacion == EstadoAsignacion.Vigente
            && datos.FechaLimiteRetiro.Date < DateTime.UtcNow.Date;

        return new AsignacionDetalleViewModel
        {
            NroAsignacion = datos.NroAsignacion,
            NroSolicitud = datos.NroSolicitud,
            Solicitante = NombreSolicitante(datos.RazonSocial, datos.Nombre, datos.Apellido),
            TipoSolicitante = datos.TipoSolicitante,
            NroEquipo = datos.NroEquipo,
            TipoEquipo = datos.TipoEquipo,
            MarcaYModelo = $"{datos.Marca} {datos.Modelo}",
            Coordinador = coordinador,
            FechaAsignacion = datos.FechaAsignacion,
            FechaLimiteRetiro = datos.FechaLimiteRetiro,
            EstadoAsignacion = datos.EstadoAsignacion,
            CambioPrioridad = !string.IsNullOrWhiteSpace(datos.JustificacionCambio),
            JustificacionCambio = datos.JustificacionCambio,
            PlazoVencido = vencido,
            FechaLiberacion = datos.FechaLiberacion,
            MotivoLiberacion = datos.MotivoLiberacion,
            Hitos = ArmarHitos(datos.EstadoAsignacion, datos.FechaAsignacion, datos.FechaLimiteRetiro, vencido,
                coordinador, fechaEntrega, datos.FechaLiberacion, datos.MotivoLiberacion),
            Liberacion = new LiberarAsignacionViewModel { NroAsignacion = datos.NroAsignacion }
        };
    }

    private static List<HitoViewModel> ArmarHitos(
        EstadoAsignacion estado, DateTime fechaAsignacion, DateTime fechaLimite, bool vencido,
        string coordinador, DateTime? fechaEntrega, DateTime? fechaLiberacion, string? motivoLiberacion)
    {
        var hitos = new List<HitoViewModel>
        {
            new()
            {
                Titulo = "Asignación registrada",
                Fecha = fechaAsignacion,
                Icono = "swap",
                Estado = EstadoHito.Completado,
                Texto = $"Registrada por {coordinador}."
            }
        };

        switch (estado)
        {
            case EstadoAsignacion.Vigente:
                hitos.Add(new HitoViewModel
                {
                    Titulo = vencido ? "Plazo de retiro vencido" : "Plazo de retiro",
                    Icono = "clock",
                    Estado = vencido ? EstadoHito.Cancelado : EstadoHito.Actual,
                    Texto = vencido
                        ? $"Venció el {fechaLimite:dd/MM/yyyy}. Solo se puede liberar."
                        : $"Hasta el {fechaLimite:dd/MM/yyyy}."
                });
                hitos.Add(new HitoViewModel { Titulo = "Entrega del equipo", Icono = "truck", Estado = EstadoHito.Pendiente });
                break;

            case EstadoAsignacion.Concretada:
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Plazo de retiro",
                    Fecha = fechaLimite,
                    Icono = "clock",
                    Estado = EstadoHito.Completado
                });
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Entrega completada",
                    Fecha = fechaEntrega,
                    Icono = "truck",
                    Estado = EstadoHito.Completado
                });
                break;

            default:
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Asignación liberada",
                    Fecha = fechaLiberacion,
                    Icono = "undo",
                    Estado = EstadoHito.Cancelado,
                    Texto = motivoLiberacion
                });
                break;
        }

        return hitos;
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

    public async Task<ResultadoAsignacion> LiberarAsync(
        string identityUserIdCoordinador, int nroAsignacion, string? motivoLiberacion)
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
                "La cuenta no está habilitada para liberar asignaciones.");
        }

        var motivo = motivoLiberacion?.Trim();

        if (string.IsNullOrEmpty(motivo))
        {
            return ResultadoAsignacion.Fallo(ErrorAsignacion.MotivoRequerido,
                "El motivo de la liberación es obligatorio.");
        }

        if (motivo.Length > MaximoMotivoLiberacion)
        {
            return ResultadoAsignacion.Fallo(ErrorAsignacion.MotivoRequerido,
                $"El motivo de la liberación no puede superar los {MaximoMotivoLiberacion} caracteres.");
        }

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            // con seguimiento: el RowVersion de los tres detecta una entrega o una liberación concurrente
            var asignacion = await _context.Asignaciones
                .Include(a => a.Equipo)
                .Include(a => a.Solicitud)
                .FirstOrDefaultAsync(a => a.NroAsignacion == nroAsignacion);

            if (asignacion is null)
            {
                return ResultadoAsignacion.Fallo(ErrorAsignacion.AsignacionNoEncontrada,
                    "No se encontró la asignación.");
            }

            if (asignacion.EstadoAsignacion != EstadoAsignacion.Vigente)
            {
                return ResultadoAsignacion.Fallo(ErrorAsignacion.AsignacionNoVigente,
                    "La asignación ya no está Vigente: se entregó o se liberó. Solo se puede liberar una asignación Vigente.");
            }

            var ahora = DateTime.UtcNow;

            asignacion.EstadoAsignacion = EstadoAsignacion.Liberada;
            asignacion.FechaLiberacion = ahora;
            asignacion.MotivoLiberacion = motivo;
            asignacion.FechaUltimaModificacion = ahora;

            asignacion.Equipo.EstadoEquipo = EstadoEquipo.Disponible;
            asignacion.Equipo.FechaUltimaModificacion = ahora;

            asignacion.Solicitud.EstadoSolicitud = EstadoSolicitud.Cerrada;
            asignacion.Solicitud.FechaCierre = ahora;
            asignacion.Solicitud.FechaUltimaModificacion = ahora;

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoAsignacion.Fallo(ErrorAsignacion.AsignacionConcurrente, MensajeConcurrenciaLiberacion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la liberación de la Asignación {NroAsignacion}", nroAsignacion);
            return ResultadoAsignacion.Fallo(ErrorAsignacion.Fallido,
                "No se pudo liberar la asignación. No se guardó nada. Intentar nuevamente.");
        }

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.Liberacion, codigoCoordinador, nameof(Asignacion),
                nroAsignacion, $"Liberación de la asignación {nroAsignacion}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar la liberación de la Asignación {NroAsignacion}", nroAsignacion);
        }

        return ResultadoAsignacion.Ok(nroAsignacion);
    }
}
