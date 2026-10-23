using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Services.Equipos;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class EquiposController : Controller
{
    private readonly IEquipoConsultaService _equipoConsultaService;
    private readonly IImagenEquipoService _imagenEquipoService;
    private readonly ILogger<EquiposController> _logger;

    public EquiposController(
        IEquipoConsultaService equipoConsultaService,
        IImagenEquipoService imagenEquipoService,
        ILogger<EquiposController> logger)
    {
        _equipoConsultaService = equipoConsultaService;
        _imagenEquipoService = imagenEquipoService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public async Task<IActionResult> Index(EquipoListadoViewModel filtros)
    {
        filtros.Orden ??= OrdenEquipo.RegistroDesc;

        await CargarListadoAsync(filtros, soloPendientesDeDiagnostico: false);

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "SoloTecnico")]
    public async Task<IActionResult> Pendientes(EquipoListadoViewModel filtros)
    {
        filtros.Orden ??= OrdenEquipo.RegistroAsc;

        await CargarListadoAsync(filtros, soloPendientesDeDiagnostico: true);

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public async Task<IActionResult> Detalle(int id)
    {
        var detalle = await _equipoConsultaService.ObtenerDetalleAsync(id);

        return detalle is null ? NotFound() : View(detalle);
    }

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public async Task<IActionResult> Historial(int id)
    {
        var historial = await _equipoConsultaService.ObtenerHistorialAsync(id);

        return historial is null ? NotFound() : View(historial);
    }

    [HttpGet]
    [Authorize(Policy = "SoloTecnico")]
    public async Task<IActionResult> CargarImagen(int id)
    {
        var equipo = await _equipoConsultaService.ObtenerDetalleAsync(id);

        if (equipo is null)
        {
            return NotFound();
        }

        return View(new CargaImagenViewModel
        {
            NroEquipo = id,
            CodigoEquipo = equipo.CodigoEquipo,
            MarcaYModelo = equipo.MarcaYModelo
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SoloTecnico")]
    public async Task<IActionResult> CargarImagen(int id, CargaImagenViewModel modelo)
    {
        var equipo = await _equipoConsultaService.ObtenerDetalleAsync(id);

        if (equipo is null)
        {
            return NotFound();
        }

        // el equipo lo define la ruta, no el formulario
        modelo.NroEquipo = id;
        modelo.CodigoEquipo = equipo.CodigoEquipo;
        modelo.MarcaYModelo = equipo.MarcaYModelo;

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado = await _imagenEquipoService.CargarAsync(modelo);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(modelo);
        }

        TempData["Mensaje"] = "La imagen se cargó correctamente.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    private async Task CargarListadoAsync(EquipoListadoViewModel filtros, bool soloPendientesDeDiagnostico)
    {
        try
        {
            var resultado = await _equipoConsultaService.ListarAsync(
                filtros.Busqueda, filtros.Estado, filtros.Tipo, soloPendientesDeDiagnostico,
                filtros.Orden!.Value, filtros.Pagina, EquipoListadoViewModel.TamanoPagina);

            filtros.Filas = resultado.Elementos;
            filtros.Total = resultado.Total;
            filtros.Pagina = resultado.Pagina;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el listado de equipos");
            filtros.ErrorAlCargar = true;
        }
    }
}
