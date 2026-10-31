using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Services.Equipos;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class EquiposController : Controller
{
    private readonly IEquipoConsultaService _equipoConsultaService;
    private readonly IImagenEquipoService _imagenEquipoService;
    private readonly IPersonalService _personalService;
    private readonly ILogger<EquiposController> _logger;

    public EquiposController(
        IEquipoConsultaService equipoConsultaService,
        IImagenEquipoService imagenEquipoService,
        IPersonalService personalService,
        ILogger<EquiposController> logger)
    {
        _equipoConsultaService = equipoConsultaService;
        _imagenEquipoService = imagenEquipoService;
        _personalService = personalService;
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
    [Authorize(Policy = "Coordinacion")]
    public async Task<IActionResult> Legajo(int id)
    {
        var legajo = await _equipoConsultaService.ObtenerLegajoAsync(id);

        return legajo is null ? NotFound() : View(legajo);
    }

    [HttpGet]
    [Authorize(Policy = "SoloTecnico")]
    public async Task<IActionResult> Fotos(int id)
    {
        var gestion = await _imagenEquipoService.ObtenerGestionAsync(id);

        return gestion is null ? NotFound() : View(gestion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SoloTecnico")]
    [ServiceFilter(typeof(LimitePeticionImagenesFilter), Order = 900)]
    public async Task<IActionResult> AgregarFotos(int id, CargaFotosViewModel modelo)
    {
        var codigoTecnico = await ObtenerCodigoTecnicoAsync();

        if (codigoTecnico is null)
        {
            return Forbid();
        }

        var archivos = modelo.Archivos
            .Select((archivo, indice) => new ArchivoImagen(archivo, indice < modelo.Angulos.Count ? modelo.Angulos[indice] : null))
            .ToList();

        var resultado = await _imagenEquipoService.AgregarAsync(id, archivos, codigoTecnico.Value);

        if (!resultado.Succeeded)
        {
            return await FotosConErroresAsync(id, resultado);
        }

        TempData["Mensaje"] = archivos.Count == 1
            ? "La foto se cargó correctamente."
            : $"Se cargaron {archivos.Count} fotos correctamente.";

        return RedirectToAction(nameof(Fotos), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SoloTecnico")]
    public async Task<IActionResult> EliminarFoto(int id, int nroImagen)
    {
        var codigoTecnico = await ObtenerCodigoTecnicoAsync();

        if (codigoTecnico is null)
        {
            return Forbid();
        }

        var resultado = await _imagenEquipoService.EliminarAsync(id, nroImagen, codigoTecnico.Value);

        if (!resultado.Succeeded)
        {
            return await FotosConErroresAsync(id, resultado);
        }

        TempData["Mensaje"] = "La foto se eliminó correctamente.";

        return RedirectToAction(nameof(Fotos), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SoloTecnico")]
    public async Task<IActionResult> ActualizarAnguloFoto(int id, int nroImagen, string? angulo)
    {
        var codigoTecnico = await ObtenerCodigoTecnicoAsync();

        if (codigoTecnico is null)
        {
            return Forbid();
        }

        var resultado = await _imagenEquipoService.ActualizarAnguloAsync(id, nroImagen, angulo, codigoTecnico.Value);

        if (!resultado.Succeeded)
        {
            return await FotosConErroresAsync(id, resultado);
        }

        TempData["Mensaje"] = "El ángulo de la foto se actualizó correctamente.";

        return RedirectToAction(nameof(Fotos), new { id });
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

    private async Task<int?> ObtenerCodigoTecnicoAsync()
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return identityUserId is null ? null : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);
    }

    private async Task<IActionResult> FotosConErroresAsync(int id, IdentityResult resultado)
    {
        foreach (var error in resultado.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        var gestion = await _imagenEquipoService.ObtenerGestionAsync(id);

        return gestion is null ? NotFound() : View("Fotos", gestion);
    }
}