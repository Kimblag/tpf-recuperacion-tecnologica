using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.Services.Configuracion;
using TPF.RecuperacionTecnologica.Web.Services.Notificaciones;
using TPF.RecuperacionTecnologica.Web.Services.Reloj;
using TPF.RecuperacionTecnologica.Web.ViewModels.Asignaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.Services.Asignaciones;

public class AsignacionService : IAsignacionService
{
    private const int MaximoJustificacion = 300;
    private const int MaximoMotivoLiberacion = 300;
    private const int TamanoLoteLiberacionAutomatica = 50;
    private const string MotivoLiberacionAutomatica = "Venció el plazo de retiro.";

    private const string MensajeConcurrenciaLiberacion =
        "La asignación cambió mientras se liberaba: otra persona pudo entregarla o liberarla. No se guardó nada. Volver a abrir el detalle e intentar nuevamente.";

    private const string MensajeConcurrencia =
        "La solicitud o el equipo cambiaron mientras se asignaba: otra persona pudo asignar el equipo, o la solicitud se editó o se canceló. No se guardó nada. Volver a abrir la pantalla e intentar nuevamente.";

    private readonly ApplicationDbContext _context;
    private readonly IConfiguracionService _configuracionService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IRelojInstitucional _reloj;
    private readonly INotificacionService _notificaciones;
    private readonly ILogger<AsignacionService> _logger;

    public AsignacionService(
        ApplicationDbContext context,
        IConfiguracionService configuracionService,
        IAuditoriaService auditoriaService,
        IRelojInstitucional reloj,
        INotificacionService notificaciones,
        ILogger<AsignacionService> logger)
    {
        _context = context;
        _configuracionService = configuracionService;
        _auditoriaService = auditoriaService;
        _reloj = reloj;
        _notificaciones = notificaciones;
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
            .Select(e => new
            {
                e.NroEquipo,
                e.TipoEquipo,
                e.Marca,
                e.Modelo,
                ImagenUrl = _context.ImagenesEquipo
                    .Where(i => i.NroEquipo == e.NroEquipo)
                    .OrderBy(i => i.FechaCarga)
                    .ThenBy(i => i.NroImagen)
                    .Select(i => i.UrlImagen)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return equipos
            .Select(e => new EquipoSeleccionableViewModel
            {
                NroEquipo = e.NroEquipo,
                TipoEquipo = e.TipoEquipo,
                MarcaYModelo = $"{e.Marca} {e.Modelo}",
                ImagenUrl = e.ImagenUrl
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
        // "hoy" es el día de la institución; los plazos se comparan contra el inicio de ese día
        var inicioHoy = _reloj.InicioDeDiaUtc();
        var finAviso = _reloj.InicioDeDiaUtc(await ObtenerDiasAvisoAsync() + 1);

        var consulta = _context.Asignaciones.AsNoTracking().AsQueryable();

        if (plazo is not null)
        {
            // el plazo solo tiene sentido en las Vigentes, sin importar el estado elegido
            consulta = consulta.Where(a => a.EstadoAsignacion == EstadoAsignacion.Vigente);

            consulta = plazo == FiltroPlazoAsignacion.Vencidas
                ? consulta.Where(a => a.FechaLimiteRetiro < inicioHoy)
                : consulta.Where(a => a.FechaLimiteRetiro >= inicioHoy && a.FechaLimiteRetiro < finAviso);
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
                    PlazoVencido = vigente && f.FechaLimiteRetiro < inicioHoy,
                    PlazoPorVencer = vigente && f.FechaLimiteRetiro >= inicioHoy && f.FechaLimiteRetiro < finAviso
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
            && _reloj.PlazoVencido(datos.FechaLimiteRetiro);

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

        await _notificaciones.AvisarAsignacionAsync(asignacion.NroAsignacion);

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

        var resultado = await LiberarEnTransaccionAsync(nroAsignacion, motivo, soloSiVencida: false);

        if (resultado.Exitoso)
        {
            await AuditarLiberacionAsync(codigoCoordinador, nroAsignacion, $"Liberación de la asignación {nroAsignacion}");
            await _notificaciones.AvisarLiberacionAsync(nroAsignacion);
        }

        return resultado;
    }

    public async Task<ResultadoLiberacionAutomatica> LiberarVencidasAsync(CancellationToken cancellationToken = default)
    {
        // el plazo vence al terminar su día: recién desde el día siguiente la asignación es liberable
        var inicioHoy = _reloj.InicioDeDiaUtc();

        var liberadas = new List<int>();
        var omitidas = 0;
        var fallidas = 0;

        // cada lote se vuelve a consultar: lo que se liberó o se entregó ya no aparece como Vigente, así que el
        // proceso avanza. Las ya intentadas se saltean para no reintentar en la misma corrida las que fallaron
        var intentadas = new HashSet<int>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var lote = await _context.Asignaciones
                .AsNoTracking()
                .Where(a => a.EstadoAsignacion == EstadoAsignacion.Vigente
                    && a.FechaLimiteRetiro < inicioHoy
                    && !intentadas.Contains(a.NroAsignacion))
                .OrderBy(a => a.FechaLimiteRetiro)
                .ThenBy(a => a.NroAsignacion)
                .Select(a => a.NroAsignacion)
                .Take(TamanoLoteLiberacionAutomatica)
                .ToListAsync(cancellationToken);

            if (lote.Count == 0)
            {
                break;
            }

            foreach (var nroAsignacion in lote)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // una falla en una asignación no frena a las demás
                var resultado = await LiberarEnTransaccionAsync(nroAsignacion, MotivoLiberacionAutomatica, soloSiVencida: true);

                // el contexto se reutiliza entre asignaciones: se limpia para no arrastrar entidades de una que falló
                _context.ChangeTracker.Clear();

                intentadas.Add(nroAsignacion);

                if (resultado.Exitoso)
                {
                    liberadas.Add(nroAsignacion);

                    // es una acción del sistema: no hay personal asociado
                    await AuditarLiberacionAsync(null, nroAsignacion,
                        $"Liberación automática de la asignación {nroAsignacion}: venció el plazo de retiro");
                    await _notificaciones.AvisarLiberacionAsync(nroAsignacion);
                }
                else if (resultado.Error is ErrorAsignacion.AsignacionNoVigente or ErrorAsignacion.AsignacionNoVencida
                    or ErrorAsignacion.AsignacionConcurrente or ErrorAsignacion.AsignacionNoEncontrada)
                {
                    // se entregó o se liberó en ese mismo momento: no es un error
                    omitidas++;
                }
                else
                {
                    fallidas++;
                }
            }
        }

        return new ResultadoLiberacionAutomatica(liberadas, omitidas, fallidas);
    }

    /// <summary>
    /// Pasa la asignación a Liberada, el equipo a Disponible y la solicitud a Cerrada, en una sola transacción.
    /// Lo comparten la liberación del Coordinador y la automática. No audita: lo hace quien la llama.
    /// </summary>
    private async Task<ResultadoAsignacion> LiberarEnTransaccionAsync(int nroAsignacion, string motivo, bool soloSiVencida)
    {
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

            // se revisa de nuevo con la fila leída: pudo cambiar desde que se armó el lote
            if (soloSiVencida && !_reloj.PlazoVencido(asignacion.FechaLimiteRetiro))
            {
                return ResultadoAsignacion.Fallo(ErrorAsignacion.AsignacionNoVencida,
                    "El plazo de retiro de la asignación todavía no venció.");
            }

            var ahora = _reloj.AhoraUtc;

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

        return ResultadoAsignacion.Ok(nroAsignacion);
    }

    private async Task AuditarLiberacionAsync(int? codigoPersonal, int nroAsignacion, string detalle)
    {
        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.Liberacion, codigoPersonal, nameof(Asignacion), nroAsignacion, detalle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar la liberación de la Asignación {NroAsignacion}", nroAsignacion);
        }
    }
}