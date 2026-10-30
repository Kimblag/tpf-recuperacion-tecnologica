using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Asignaciones;
using TPF.RecuperacionTecnologica.Web.Services.Configuracion;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.ViewModels.Asignaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class AsignacionesController : Controller
{
    // se usa solo si todavía no existe la fila de configuración
    private const int DiasLimiteRetiroPorDefecto = 10;

    private readonly IDonacionService _donacionService;
    private readonly IPersonalService _personalService;
    private readonly IConfiguracionService _configuracionService;
    private readonly IAsignacionService _asignacionService;
    private readonly ILogger<AsignacionesController> _logger;

    public AsignacionesController(
        IDonacionService donacionService,
        IPersonalService personalService,
        IConfiguracionService configuracionService,
        IAsignacionService asignacionService,
        ILogger<AsignacionesController> logger)
    {
        _donacionService = donacionService;
        _personalService = personalService;
        _configuracionService = configuracionService;
        _asignacionService = asignacionService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public async Task<IActionResult> Index(AsignacionListadoViewModel filtros)
    {
        if (!Request.Query.ContainsKey(nameof(AsignacionListadoViewModel.Estado)))
        {
            filtros.Estado = EstadoAsignacion.Vigente;
        }

        try
        {
            var resultado = await _asignacionService.ListarAsync(
                filtros.Busqueda, filtros.Estado, filtros.Plazo,
                filtros.Pagina, AsignacionListadoViewModel.TamanoPagina);

            filtros.Filas = resultado.Elementos;
            filtros.Total = resultado.Total;
            filtros.Pagina = resultado.Pagina;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el listado de asignaciones");
            filtros.ErrorAlCargar = true;
        }

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public async Task<IActionResult> Detalle(int id)
    {
        var asignacion = await _asignacionService.ObtenerDetalleAsync(id);

        if (asignacion is null)
        {
            return NotFound();
        }

        return View(asignacion);
    }

    [HttpGet]
    [Authorize(Policy = "SoloCoordinador")]
    public async Task<IActionResult> Asignar(int? nroEquipo, string? busquedaEquipo)
    {
        var formulario = new AsignarEquipoViewModel { NroEquipo = nroEquipo };
        formulario.Equipo.Busqueda = busquedaEquipo;

        await CompletarFormularioAsync(formulario);

        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SoloCoordinador")]
    public async Task<IActionResult> Asignar(
        [Bind("NroEquipo,NroSolicitudSeleccionada,JustificacionCambio")] AsignarEquipoViewModel formulario)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        if (formulario.NroEquipo is not null && ModelState.IsValid)
        {
            var resultado = await _asignacionService.AsignarAsync(
                identityUserId, formulario.NroEquipo.Value, formulario.NroSolicitudSeleccionada!.Value, formulario.JustificacionCambio);

            if (resultado.Exitoso)
            {
                TempData["Mensaje"] = "La asignación se registró correctamente.";
                return RedirectToAction(nameof(Detalle), new { id = resultado.NroAsignacion });
            }

            ModelState.AddModelError(string.Empty, resultado.Mensaje!);
        }

        await CompletarFormularioAsync(formulario);

        return View(formulario);
    }

    private const int MaximoResultadosEquipo = 10;

    private async Task CompletarFormularioAsync(AsignarEquipoViewModel formulario)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var codigoPersonal = identityUserId is null ? null : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);
        var nombre = User.FindFirst(ClaimsSesion.Nombre)?.Value ?? string.Empty;
        var diasLimiteRetiro = (await _configuracionService.ObtenerAsync())?.DiasLimiteRetiro
            ?? DiasLimiteRetiroPorDefecto;

        formulario.Coordinador = codigoPersonal is null ? nombre : $"{nombre} · PER-{codigoPersonal:D4}";
        formulario.FechaAsignacion = DateTime.Today;
        formulario.DiasLimiteRetiro = diasLimiteRetiro;

        if (formulario.NroEquipo is null)
        {
            var selector = formulario.Equipo;

            // solo se ofrecen equipos con solicitudes pendientes; sin búsqueda se precargan los más recientes
            selector.BusquedaRealizada = true;
            selector.Resultados = await _asignacionService.ObtenerEquiposConSolicitudPendienteAsync(
                selector.Busqueda, MaximoResultadosEquipo);

            return;
        }

        var equipo = await _donacionService.ObtenerDetalleDisponibleAsync(formulario.NroEquipo.Value);

        if (equipo is null)
        {
            formulario.EquipoDisponible = false;
            return;
        }

        formulario.TipoEquipo = equipo.TipoEquipo;
        formulario.MarcaYModelo = equipo.MarcaYModelo;
        formulario.Candidatas = await _asignacionService.ObtenerCandidatasAsync(formulario.NroEquipo.Value);

        // si no llegó una elección válida, se preselecciona la sugerida
        if (formulario.NroSolicitudSeleccionada is null
            || formulario.Candidatas.All(c => c.NroSolicitud != formulario.NroSolicitudSeleccionada))
        {
            formulario.NroSolicitudSeleccionada = formulario.Candidatas.FirstOrDefault()?.NroSolicitud;
        }
    }
}
