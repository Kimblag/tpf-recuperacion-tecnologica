using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class DonacionesController : Controller
{
    private readonly IDonacionService _donacionService;

    public DonacionesController(IDonacionService donacionService)
    {
        _donacionService = donacionService;
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Index(MisDonacionesViewModel filtros)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return NotFound();
        }

        filtros.Filas = await _donacionService.ListarAsync(
            identityUserId, filtros.Busqueda, filtros.Estado, filtros.Tipo, filtros.Condicion);
        filtros.Total = filtros.Filas.Count;

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Detalle(int id)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var donacion = identityUserId is null
            ? null
            : await _donacionService.ObtenerDetalleAsync(identityUserId, id);

        return donacion is null ? NotFound() : View(donacion);
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public IActionResult Registrar()
    {
        return View(new RegistroDonacionViewModel());
    }

    // el donante se identifica por la sesión, no por un dato del formulario.
    // el parámetro no se llama "modelo" porque el ViewModel tiene la propiedad Modelo y confundiría al model binding
    [HttpPost]
    [Authorize(Policy = "UsuarioExterno")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegistroDonacionViewModel donacion)
    {
        if (!ModelState.IsValid)
        {
            return View(donacion);
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var nroEquipo = identityUserId is null ? null : await _donacionService.RegistrarAsync(identityUserId, donacion);

        return nroEquipo is null
            ? NotFound()
            : RedirectToAction(nameof(Confirmacion), new { id = nroEquipo });
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Confirmacion(int id)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var donacion = identityUserId is null
            ? null
            : await _donacionService.ObtenerDetalleAsync(identityUserId, id);

        return donacion is null ? NotFound() : View(id);
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Editar(int id)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var donacion = identityUserId is null
            ? null
            : await _donacionService.ObtenerParaEdicionAsync(identityUserId, id);

        // una donación inexistente y una de otro Usuario responden igual
        if (donacion is null)
        {
            return NotFound();
        }

        return donacion.EstadoEquipo == EstadoEquipo.PendienteRecepcion
            ? View(donacion)
            : RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [Authorize(Policy = "UsuarioExterno")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EdicionDonacionViewModel donacion)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var original = identityUserId is null
            ? null
            : await _donacionService.ObtenerParaEdicionAsync(identityUserId, donacion.NroEquipo);

        if (original is null)
        {
            return NotFound();
        }

        // no podemos editar el tipo!! se descarta lo que llegue del formulario y se muestra el guardado
        ModelState.Remove(nameof(EdicionDonacionViewModel.TipoEquipo));
        donacion.TipoEquipo = original.TipoEquipo;
        donacion.Donante = original.Donante;
        donacion.FechaRegistro = original.FechaRegistro;
        donacion.EstadoEquipo = original.EstadoEquipo;

        if (!ModelState.IsValid)
        {
            return View(donacion);
        }

        var resultado = await _donacionService.EditarAsync(identityUserId!, donacion);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(donacion);
        }

        TempData["Mensaje"] = "Los datos de la donación se actualizaron correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = donacion.NroEquipo });
    }

    [HttpPost]
    [Authorize(Policy = "UsuarioExterno")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(CancelarDonacionViewModel donacion)
    {
        // TODO: reemplazar por CancelarAsync del servicio de donaciones y redirigir a Mis donaciones con el aviso de éxito
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var original = identityUserId is null
            ? null
            : await _donacionService.ObtenerDetalleAsync(identityUserId, donacion.NroEquipo);

        if (original is null)
        {
            return NotFound();
        }

        if (!original.PuedeModificarse)
        {
            return RedirectToAction(nameof(Detalle), new { id = original.NroEquipo });
        }

        ModelState.AddModelError(string.Empty, "La cancelación de donaciones todavía no está implementada.");
        return View(nameof(Detalle), original);
    }
}