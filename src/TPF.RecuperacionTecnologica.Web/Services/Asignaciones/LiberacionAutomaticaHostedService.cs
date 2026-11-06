using Microsoft.Extensions.Options;
using TPF.RecuperacionTecnologica.Web.Services.Notificaciones;

namespace TPF.RecuperacionTecnologica.Web.Services.Asignaciones;

/// <summary>
/// Libera cada cierto tiempo las asignaciones con el plazo de retiro vencido.
/// </summary>
public class LiberacionAutomaticaHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<LiberacionAutomaticaOptions> _options;
    private readonly ILogger<LiberacionAutomaticaHostedService> _logger;

    public LiberacionAutomaticaHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<LiberacionAutomaticaOptions> options,
        ILogger<LiberacionAutomaticaHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opciones = _options.Value;

        if (!opciones.Habilitada)
        {
            _logger.LogInformation("La liberación automática de asignaciones vencidas está deshabilitada.");
            return;
        }

        var intervalo = TimeSpan.FromMinutes(Math.Max(1, opciones.IntervaloMinutos));

        _logger.LogInformation("Liberación automática de asignaciones vencidas cada {Intervalo}.", intervalo);

        // una corrida al arrancar y después una por intervalo
        using var temporizador = new PeriodicTimer(intervalo);

        do
        {
            await EjecutarCicloAsync(stoppingToken);
        }
        while (await EsperarSiguienteAsync(temporizador, stoppingToken));
    }

    private static async Task<bool> EsperarSiguienteAsync(PeriodicTimer temporizador, CancellationToken stoppingToken)
    {
        try
        {
            return await temporizador.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task EjecutarCicloAsync(CancellationToken stoppingToken)
    {
        try
        {
            // un scope por ciclo: el servicio de asignaciones es scoped y comparte un DbContext
            using var scope = _scopeFactory.CreateScope();
            var servicio = scope.ServiceProvider.GetRequiredService<IAsignacionService>();

            var resultado = await servicio.LiberarVencidasAsync(stoppingToken);

            // en el mismo ciclo, el recordatorio de plazo próximo a vencer (una sola vez por asignación)
            var notificaciones = scope.ServiceProvider.GetRequiredService<INotificacionService>();
            var avisos = await notificaciones.EnviarAvisosDeVencimientoAsync(stoppingToken);

            if (resultado.Liberadas.Count > 0 || resultado.Omitidas > 0 || resultado.Fallidas > 0 || avisos > 0)
            {
                _logger.LogInformation(
                    "Liberación automática: {Liberadas} liberadas, {Omitidas} omitidas, {Fallidas} con error; {Avisos} recordatorios enviados.",
                    resultado.Liberadas.Count, resultado.Omitidas, resultado.Fallidas, avisos);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            // un ciclo fallido no debe tumbar el servicio: se reintenta en el próximo
            _logger.LogError(ex, "Falló la liberación automática de asignaciones vencidas.");
        }
    }
}
