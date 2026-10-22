using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

// el catálogo lo ve cualquier persona con sesión, sea Usuario o Personal
[Authorize]
public class CatalogoController : Controller
{
    private readonly IDonacionService _donacionService;
    private readonly ILogger<CatalogoController> _logger;

    public CatalogoController(IDonacionService donacionService, ILogger<CatalogoController> logger)
    {
        _donacionService = donacionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CatalogoListadoViewModel filtros)
    {
        try
        {
            filtros.Filas = await _donacionService.ObtenerDisponiblesAsync(filtros.Tipo);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el catálogo de equipos disponibles");
            filtros.ErrorAlCargar = true;
        }

        return View(filtros);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var equipo = await _donacionService.ObtenerDetalleDisponibleAsync(id);

        return equipo is null ? NotFound() : View(equipo);
    }
}
