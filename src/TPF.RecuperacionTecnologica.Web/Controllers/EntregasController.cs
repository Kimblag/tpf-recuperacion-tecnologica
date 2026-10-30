using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Entregas;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Entregas;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class EntregasController : Controller
{
    private const int MaximoCandidatas = 10;

    private readonly IPersonalService _personalService;
    private readonly IEntregaService _entregaService;
    private readonly ILogger<EntregasController> _logger;

    public EntregasController(
        IPersonalService personalService, IEntregaService entregaService, ILogger<EntregasController> logger)
    {
        _personalService = personalService;
        _entregaService = entregaService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public async Task<IActionResult> Index(EntregaListadoViewModel filtros)
    {
        // con un rango de fechas invertido se muestra el mensaje sin consultar
        if (!ModelState.IsValid)
        {
            return View(filtros);
        }

        try
        {
            var resultado = await _entregaService.ListarAsync(
                filtros.Busqueda, filtros.FechaDesde, filtros.FechaHasta,
                filtros.Pagina, EntregaListadoViewModel.TamanoPagina);

            filtros.Filas = resultado.Elementos;
            filtros.Total = resultado.Total;
            filtros.Pagina = resultado.Pagina;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el listado de entregas");
            filtros.ErrorAlCargar = true;
        }

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public async Task<IActionResult> Detalle(int id)
    {
        var detalle = await _entregaService.ObtenerDetalleAsync(id);

        return detalle is null ? NotFound() : View(detalle);
    }

    [HttpGet]
    [Authorize(Policy = "SoloCoordinador")]
    public async Task<IActionResult> Registrar(int? nroAsignacion, string? busqueda)
    {
        var formulario = nroAsignacion is null
            ? new RegistroEntregaViewModel
            {
                Situacion = SituacionEntrega.ElegirAsignacion,
                Busqueda = busqueda?.Trim()
            }
            : await _entregaService.ObtenerParaRegistrarAsync(nroAsignacion.Value)
                ?? new RegistroEntregaViewModel { NroAsignacion = nroAsignacion, Situacion = SituacionEntrega.NoEncontrada };

        await CompletarFormularioAsync(formulario);

        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SoloCoordinador")]
    public async Task<IActionResult> Registrar(
        [Bind("NroAsignacion,NombreQuienRetira,DocumentoQuienRetira")] RegistroEntregaViewModel formulario)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        if (formulario.NroAsignacion is null)
        {
            return RedirectToAction(nameof(Registrar));
        }

        if (ModelState.IsValid)
        {
            var resultado = await _entregaService.RegistrarAsync(
                identityUserId, formulario.NroAsignacion.Value, formulario.NombreQuienRetira, formulario.DocumentoQuienRetira);

            if (resultado.Exitoso)
            {
                TempData["Mensaje"] = "La entrega se registró correctamente.";
                return RedirectToAction("Detalle", "Asignaciones", new { id = formulario.NroAsignacion });
            }

            ModelState.AddModelError(string.Empty, resultado.Mensaje!);
        }

        // se vuelven a leer los datos de la asignación: pudo cambiar de estado mientras se completaba el formulario
        var datos = await _entregaService.ObtenerParaRegistrarAsync(formulario.NroAsignacion.Value);

        if (datos is null)
        {
            datos = new RegistroEntregaViewModel { NroAsignacion = formulario.NroAsignacion, Situacion = SituacionEntrega.NoEncontrada };
        }

        datos.NombreQuienRetira = formulario.NombreQuienRetira;
        datos.DocumentoQuienRetira = formulario.DocumentoQuienRetira;

        await CompletarFormularioAsync(datos);

        return View(datos);
    }

    private async Task CompletarFormularioAsync(RegistroEntregaViewModel formulario)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var codigoPersonal = identityUserId is null ? null : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);
        var nombre = User.FindFirst(ClaimsSesion.Nombre)?.Value ?? string.Empty;

        formulario.Coordinador = codigoPersonal is null ? nombre : $"{nombre} · PER-{codigoPersonal:D4}";
        formulario.FechaEntrega = DateTime.Today;

        if (formulario.Situacion == SituacionEntrega.ElegirAsignacion)
        {
            formulario.Candidatas = await _entregaService.BuscarAsignacionesVigentesAsync(formulario.Busqueda, MaximoCandidatas);
        }
    }
}
