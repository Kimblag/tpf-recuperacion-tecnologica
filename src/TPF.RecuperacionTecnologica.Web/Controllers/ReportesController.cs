using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Services.Reportes;
using TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize(Policy = "SoloAdministrador")]
public class ReportesController : Controller
{
    private readonly IReporteService _reporteService;
    private readonly ILogger<ReportesController> _logger;

    public ReportesController(IReporteService reporteService, ILogger<ReportesController> logger)
    {
        _reporteService = reporteService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new GenerarReporteViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Generar(GenerarReporteViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), modelo);
        }

        var rango = new { desde = modelo.Desde, hasta = modelo.Hasta };

        return modelo.Tipo switch
        {
            TipoReporte.EntregasPorPeriodo => RedirectToAction(nameof(EntregasPorPeriodo), rango),
            TipoReporte.TiposSolicitados => RedirectToAction(nameof(TiposSolicitados), rango),
            _ => RedirectToAction(nameof(EquiposPorEstado), rango)
        };
    }

    // los accesos rápidos llegan sin fechas e incluyen todo lo disponible
    [HttpGet]
    public async Task<IActionResult> EquiposPorEstado(DateTime? desde, DateTime? hasta)
    {
        if (RangoInvertido(desde, hasta, TipoReporte.EquiposPorEstado) is { } rechazo)
        {
            return rechazo;
        }

        var modelo = new ReporteEquiposPorEstadoViewModel { Desde = desde, Hasta = hasta };

        try
        {
            modelo.Estados = await _reporteService.EquiposPorEstadoAsync(desde, hasta);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo generar el informe de equipos por estado");
            modelo.ErrorAlCargar = true;
        }

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> EntregasPorPeriodo(DateTime? desde, DateTime? hasta, AgrupacionPeriodo agrupacion = AgrupacionPeriodo.Mes)
    {
        if (RangoInvertido(desde, hasta, TipoReporte.EntregasPorPeriodo) is { } rechazo)
        {
            return rechazo;
        }

        var modelo = new ReporteEntregasPorPeriodoViewModel { Desde = desde, Hasta = hasta, Agrupacion = agrupacion };

        try
        {
            modelo.Filas = await _reporteService.EntregasPorPeriodoAsync(desde, hasta, agrupacion);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo generar el informe de entregas por período");
            modelo.ErrorAlCargar = true;
        }

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> TiposSolicitados(DateTime? desde, DateTime? hasta)
    {
        if (RangoInvertido(desde, hasta, TipoReporte.TiposSolicitados) is { } rechazo)
        {
            return rechazo;
        }

        var modelo = new ReporteTiposSolicitadosViewModel { Desde = desde, Hasta = hasta };

        try
        {
            modelo.Filas = await _reporteService.TiposSolicitadosAsync(desde, hasta);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo generar el informe de tipos más solicitados");
            modelo.ErrorAlCargar = true;
        }

        return View(modelo);
    }

    // un rango invertido escrito a mano en la dirección vuelve al formulario con el mismo mensaje que su validación
    private ViewResult? RangoInvertido(DateTime? desde, DateTime? hasta, TipoReporte tipo)
    {
        if (desde is null || hasta is null || desde.Value.Date <= hasta.Value.Date)
        {
            return null;
        }

        ModelState.AddModelError(nameof(GenerarReporteViewModel.Hasta), "La fecha hasta no puede ser anterior a la fecha desde.");

        return View(nameof(Index), new GenerarReporteViewModel { Desde = desde, Hasta = hasta, Tipo = tipo });
    }
}
