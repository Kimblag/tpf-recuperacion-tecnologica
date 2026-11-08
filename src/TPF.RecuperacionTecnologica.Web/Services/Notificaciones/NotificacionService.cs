using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Configuracion;
using TPF.RecuperacionTecnologica.Web.Services.Email;
using TPF.RecuperacionTecnologica.Web.Services.Reloj;

namespace TPF.RecuperacionTecnologica.Web.Services.Notificaciones;

public class NotificacionService : INotificacionService
{
    private const int DiasAvisoVencimientoPorDefecto = 3;
    private const string NombreOrganizacionPorDefecto = "Recuperación Tecnológica";

    private sealed record Destinatario(
        string Correo, string Nombre, int NroSolicitud, string Equipo, DateTime FechaLimiteRetiro, string? MotivoLiberacion);

    private readonly ApplicationDbContext _context;
    private readonly IConfiguracionService _configuracionService;
    private readonly IEmailSender _emailSender;
    private readonly IRelojInstitucional _reloj;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificacionService> _logger;

    public NotificacionService(
        ApplicationDbContext context,
        IConfiguracionService configuracionService,
        IEmailSender emailSender,
        IRelojInstitucional reloj,
        IConfiguration configuration,
        ILogger<NotificacionService> logger)
    {
        _context = context;
        _configuracionService = configuracionService;
        _emailSender = emailSender;
        _reloj = reloj;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task AvisarAsignacionAsync(int nroAsignacion)
    {
        try
        {
            var destinatario = await ObtenerDestinatarioAsync(nroAsignacion);

            if (destinatario is null)
            {
                return;
            }

            var (organizacion, enlace) = await ObtenerContextoAsync();
            var fechaLimite = _reloj.FechaLocal(destinatario.FechaLimiteRetiro);

            EnviarEnSegundoPlano(destinatario.Correo, $"Equipo asignado - {organizacion}",
                PlantillaNotificacion.Asignacion(
                    organizacion, destinatario.Nombre, destinatario.NroSolicitud, destinatario.Equipo, fechaLimite, enlace),
                $"la asignación {nroAsignacion}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo preparar el aviso de asignación de la Asignación {NroAsignacion}", nroAsignacion);
        }
    }

    public async Task AvisarLiberacionAsync(int nroAsignacion)
    {
        try
        {
            var destinatario = await ObtenerDestinatarioAsync(nroAsignacion);

            if (destinatario is null)
            {
                return;
            }

            var (organizacion, enlace) = await ObtenerContextoAsync();

            EnviarEnSegundoPlano(destinatario.Correo, $"Solicitud cerrada - {organizacion}",
                PlantillaNotificacion.Liberacion(
                    organizacion, destinatario.Nombre, destinatario.NroSolicitud, destinatario.Equipo,
                    destinatario.MotivoLiberacion ?? "No se informó un motivo.", enlace),
                $"la asignación {nroAsignacion}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo preparar el aviso de liberación de la Asignación {NroAsignacion}", nroAsignacion);
        }
    }

    public Task AvisarCierrePorEntregaAsync(IEnumerable<int> nroSolicitudes) =>
    AvisarCierreDeSolicitudesAsync(nroSolicitudes, PlantillaNotificacion.CierrePorEntrega, "entrega");

    public Task AvisarCierrePorBajaAsync(IEnumerable<int> nroSolicitudes) =>
        AvisarCierreDeSolicitudesAsync(nroSolicitudes, PlantillaNotificacion.CierrePorBaja, "baja");

    private async Task AvisarCierreDeSolicitudesAsync(
        IEnumerable<int> nroSolicitudes,
        Func<string, string, int, string, string?, string> plantilla,
        string causa)
    {
        var ids = nroSolicitudes.ToList();

        if (ids.Count == 0)
        {
            return;
        }

        try
        {
            var filas = await _context.Solicitudes
                .AsNoTracking()
                .Where(s => ids.Contains(s.NroSolicitud) && s.Usuario.EstadoUsuario == EstadoUsuario.Activo)
                .Select(s => new
                {
                    s.NroSolicitud,
                    s.Usuario.CorreoElectronico,
                    s.Usuario.RazonSocial,
                    s.Usuario.Nombre,
                    s.Usuario.Apellido,
                    Equipo = s.EquipoRequerido.Marca + " " + s.EquipoRequerido.Modelo
                })
                .ToListAsync();

            var (organizacion, enlace) = await ObtenerContextoAsync();

            foreach (var fila in filas.Where(f => !string.IsNullOrWhiteSpace(f.CorreoElectronico)))
            {
                EnviarEnSegundoPlano(fila.CorreoElectronico, $"Solicitud cerrada - {organizacion}",
                    plantilla(
                        organizacion, NombreDe(fila.RazonSocial, fila.Nombre, fila.Apellido), fila.NroSolicitud, fila.Equipo, enlace),
                    $"la solicitud {fila.NroSolicitud}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo preparar el aviso de cierre por {Causa} de las Solicitudes {Solicitudes}", causa, ids);
        }
    }

    public async Task<int> EnviarAvisosDeVencimientoAsync(CancellationToken cancellationToken = default)
    {
        var enviados = 0;

        try
        {
            var diasAviso = (await _configuracionService.ObtenerAsync())?.DiasAvisoVencimiento ?? DiasAvisoVencimientoPorDefecto;

            // el plazo vence al terminar su día: entra en el aviso si ese día cae entre hoy y hoy + días de aviso
            var inicioHoy = _reloj.InicioDeDiaUtc();
            var finAviso = _reloj.InicioDeDiaUtc(diasAviso + 1);

            var pendientes = await _context.Asignaciones
                .AsNoTracking()
                .Where(a => a.EstadoAsignacion == EstadoAsignacion.Vigente
                    && a.FechaAvisoVencimiento == null
                    && a.FechaLimiteRetiro >= inicioHoy
                    && a.FechaLimiteRetiro < finAviso)
                .OrderBy(a => a.FechaLimiteRetiro)
                .Select(a => a.NroAsignacion)
                .ToListAsync(cancellationToken);

            if (pendientes.Count == 0)
            {
                return 0;
            }

            var (organizacion, enlace) = await ObtenerContextoAsync();

            foreach (var nroAsignacion in pendientes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (await EnviarAvisoDeVencimientoAsync(nroAsignacion, organizacion, enlace))
                {
                    enviados++;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falló el envío de los avisos de plazo próximo a vencer");
        }

        return enviados;
    }

    private async Task<bool> EnviarAvisoDeVencimientoAsync(int nroAsignacion, string organizacion, string? enlace)
    {
        try
        {
            var destinatario = await ObtenerDestinatarioAsync(nroAsignacion);

            if (destinatario is null)
            {
                return false;
            }

            // se reserva el aviso antes de enviar: si otra corrida llega al mismo tiempo, solo una lo manda
            var ahora = _reloj.AhoraUtc;

            var reservadas = await _context.Asignaciones
                .Where(a => a.NroAsignacion == nroAsignacion
                    && a.EstadoAsignacion == EstadoAsignacion.Vigente
                    && a.FechaAvisoVencimiento == null)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.FechaAvisoVencimiento, (DateTime?)ahora));

            if (reservadas == 0)
            {
                return false;
            }

            try
            {
                await _emailSender.SendEmailAsync(
                    destinatario.Correo, $"El plazo de retiro está por vencer - {organizacion}",
                    PlantillaNotificacion.VencimientoProximo(
                        organizacion, destinatario.Nombre, destinatario.NroSolicitud, destinatario.Equipo,
                        _reloj.FechaLocal(destinatario.FechaLimiteRetiro), enlace));

                return true;
            }
            catch (Exception ex)
            {
                // no llegó: se libera la reserva para reintentar en la próxima corrida
                _logger.LogError(ex, "No se pudo enviar el aviso de plazo próximo a vencer de la Asignación {NroAsignacion}", nroAsignacion);

                await _context.Asignaciones
                    .Where(a => a.NroAsignacion == nroAsignacion)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.FechaAvisoVencimiento, (DateTime?)null));

                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo preparar el aviso de plazo próximo a vencer de la Asignación {NroAsignacion}", nroAsignacion);
            return false;
        }
    }

    /// <summary>
    /// Datos de quien recibe el aviso. Devuelve null si la cuenta está dada de baja o no tiene correo.
    /// Una cuenta invitada que todavía no activó su contraseña sí recibe el aviso.
    /// </summary>
    private async Task<Destinatario?> ObtenerDestinatarioAsync(int nroAsignacion)
    {
        var fila = await _context.Asignaciones
            .AsNoTracking()
            .Where(a => a.NroAsignacion == nroAsignacion)
            .Select(a => new
            {
                a.NroSolicitud,
                a.FechaLimiteRetiro,
                a.MotivoLiberacion,
                Equipo = a.Equipo.Marca + " " + a.Equipo.Modelo,
                a.Solicitud.Usuario.CorreoElectronico,
                a.Solicitud.Usuario.EstadoUsuario,
                a.Solicitud.Usuario.RazonSocial,
                a.Solicitud.Usuario.Nombre,
                a.Solicitud.Usuario.Apellido
            })
            .FirstOrDefaultAsync();

        if (fila is null || fila.EstadoUsuario != EstadoUsuario.Activo || string.IsNullOrWhiteSpace(fila.CorreoElectronico))
        {
            return null;
        }

        return new Destinatario(
            fila.CorreoElectronico, NombreDe(fila.RazonSocial, fila.Nombre, fila.Apellido),
            fila.NroSolicitud, fila.Equipo, fila.FechaLimiteRetiro, fila.MotivoLiberacion);
    }

    private static string NombreDe(string? razonSocial, string? nombre, string? apellido) =>
        !string.IsNullOrWhiteSpace(razonSocial) ? razonSocial : $"{nombre} {apellido}".Trim();

    private async Task<(string Organizacion, string? Enlace)> ObtenerContextoAsync(string ruta = "Solicitudes")
    {
        var nombre = (await _configuracionService.ObtenerAsync())?.NombreOrganizacion;
        var organizacion = string.IsNullOrWhiteSpace(nombre) ? NombreOrganizacionPorDefecto : nombre.Trim();

        // los procesos en segundo plano no tienen un pedido web: el dominio sale siempre de la configuración
        var urlBase = _configuration["Aplicacion:UrlBase"];

        if (string.IsNullOrWhiteSpace(urlBase))
        {
            _logger.LogWarning("El aviso se envía sin enlace: falta configurar Aplicacion:UrlBase (variable de entorno Aplicacion__UrlBase)");
            return (organizacion, null);
        }

        return (organizacion, urlBase.TrimEnd('/') + "/" + ruta);
    }

    private void EnviarEnSegundoPlano(string destinatario, string asunto, string cuerpo, string referencia)
    {
        // se envía después de confirmar la operación y sin esperar: si falla, solo queda en el log
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailSender.SendEmailAsync(destinatario, asunto, cuerpo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el aviso de {Referencia}", referencia);
            }
        });
    }

    public async Task AvisarEntregaAlDonanteAsync(int nroEntrega)
    {
        try
        {
            var fila = await _context.Entregas
                .AsNoTracking()
                .Where(e => e.NroEntrega == nroEntrega)
                .Select(e => new
                {
                    e.NroEquipo,
                    e.FechaEntrega,
                    Equipo = e.Equipo.Marca + " " + e.Equipo.Modelo,
                    e.Solicitud.TipoSolicitante,
                    e.Equipo.UsuarioDonante.CorreoElectronico,
                    e.Equipo.UsuarioDonante.EstadoUsuario,
                    e.Equipo.UsuarioDonante.RazonSocial,
                    e.Equipo.UsuarioDonante.Nombre,
                    e.Equipo.UsuarioDonante.Apellido
                })
                .FirstOrDefaultAsync();

            if (fila is null || fila.EstadoUsuario != EstadoUsuario.Activo || string.IsNullOrWhiteSpace(fila.CorreoElectronico))
            {
                return;
            }

            var destinatario = fila.TipoSolicitante switch
            {
                TipoSolicitante.Escuela => "una escuela",
                TipoSolicitante.OrganizacionSocial => "una organización social",
                _ => "una persona particular"
            };

            var (organizacion, enlace) = await ObtenerContextoAsync("Donaciones");

            EnviarEnSegundoPlano(fila.CorreoElectronico, $"Tu equipo fue entregado - {organizacion}",
                PlantillaNotificacion.EntregaAlDonante(
                    organizacion, NombreDe(fila.RazonSocial, fila.Nombre, fila.Apellido), fila.NroEquipo, fila.Equipo,
                    destinatario, _reloj.FechaLocal(fila.FechaEntrega), enlace),
                $"la entrega {nroEntrega}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo preparar el aviso de entrega al donante de la Entrega {NroEntrega}", nroEntrega);
        }
    }
}