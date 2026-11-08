using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.Services.Exportacion;
using TPF.RecuperacionTecnologica.Web.Services.Reloj;
using TPF.RecuperacionTecnologica.Web.ViewModels.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize(Policy = "SoloAdministrador")]
public class AuditoriaController : Controller
{
    private readonly IAuditoriaService _auditoriaService;
    private readonly IExportacionService _exportacionService;
    private readonly IRelojInstitucional _reloj;
    private readonly ILogger<AuditoriaController> _logger;

    public AuditoriaController(
        IAuditoriaService auditoriaService,
        IExportacionService exportacionService,
        IRelojInstitucional reloj,
        ILogger<AuditoriaController> logger)
    {
        _auditoriaService = auditoriaService;
        _exportacionService = exportacionService;
        _reloj = reloj;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(AuditoriaListadoViewModel filtros)
    {
        // con filtros inválidos (por ejemplo, un rango de fechas invertido) se muestra el mensaje sin consultar
        if (!ModelState.IsValid)
        {
            return View(filtros);
        }

        try
        {
            var resultado = await _auditoriaService.ConsultarAsync(
                filtros.FechaDesde, filtros.FechaHasta, filtros.Operacion, filtros.NumeroCodigoPersonal,
                filtros.Entidad, filtros.NumeroIdentificador, filtros.Pagina, AuditoriaListadoViewModel.TamanoPagina);

            filtros.Filas = resultado.Elementos;
            filtros.Total = resultado.Total;
            filtros.Pagina = resultado.Pagina;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el registro de auditoría");
            filtros.ErrorAlCargar = true;
        }

        return View(filtros);
    }

    [HttpGet]
    public async Task<IActionResult> Exportar(AuditoriaListadoViewModel filtros, FormatoExportacion formato)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), filtros);
        }

        try
        {
            var resultado = await _auditoriaService.ConsultarAsync(
                filtros.FechaDesde, filtros.FechaHasta, filtros.Operacion, filtros.NumeroCodigoPersonal,
                filtros.Entidad, filtros.NumeroIdentificador, 1, AuditoriaListadoViewModel.LimiteExportacion);

            filtros.Filas = resultado.Elementos;
            filtros.Total = resultado.Total;

            var archivo = await _exportacionService.ExportarAsync(filtros.ATabla(_reloj), formato);

            return File(archivo.Contenido, archivo.TipoContenido, archivo.NombreArchivo);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo exportar el registro de auditoría");
            TempData["MensajeError"] = "No se pudo generar el archivo. Intentá de nuevo en unos minutos.";

            return RedirectToAction(nameof(Index), new
            {
                FechaDesde = filtros.FechaDesde?.ToString("yyyy-MM-dd"),
                FechaHasta = filtros.FechaHasta?.ToString("yyyy-MM-dd"),
                filtros.Operacion,
                filtros.CodigoPersonal,
                filtros.Entidad,
                filtros.Identificador
            });
        }
    }
}
