using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Auditoria;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize(Policy = "SoloAdministrador")]
public class AuditoriaController : Controller
{
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<AuditoriaController> _logger;

    public AuditoriaController(IAuditoriaService auditoriaService, ILogger<AuditoriaController> logger)
    {
        _auditoriaService = auditoriaService;
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
}
