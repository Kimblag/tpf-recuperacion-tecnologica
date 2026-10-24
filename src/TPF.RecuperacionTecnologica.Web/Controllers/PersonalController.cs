using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Personal;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize(Policy = "SoloAdministrador")]
public class PersonalController : Controller
{
    private readonly IPersonalService _personalService;

    public PersonalController(IPersonalService personalService)
    {
        _personalService = personalService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? busqueda, RolPersonal? rol, EstadoPersonal? estado)
    {
        return View(await _personalService.ListarAsync(busqueda, rol, estado));
    }

    [HttpGet]
    public IActionResult Alta()
    {
        return View(new AltaPersonalViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Alta(AltaPersonalViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var codigoAdministrador = identityUserId is null
            ? null
            : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);

        if (codigoAdministrador is null)
        {
            return Forbid();
        }

        var resultado = await _personalService.AltaAsync(modelo, codigoAdministrador.Value);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(modelo);
        }

        TempData["Mensaje"] = "El personal se dio de alta correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var personal = await _personalService.ObtenerDetalleAsync(id);

        if (personal is null)
        {
            return NotFound();
        }

        await MarcarCuentaPropiaAsync(personal);

        return View(personal);
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var personal = await _personalService.ObtenerDetalleAsync(id);

        if (personal is null)
        {
            return NotFound();
        }

        return View(new EdicionPersonalViewModel
        {
            CodigoPersonal = personal.CodigoPersonal,
            Dni = personal.Dni,
            Nombre = personal.Nombre,
            Apellido = personal.Apellido,
            Telefono = personal.Telefono,
            RolAsignado = personal.RolAsignado,
            CorreoElectronico = personal.CorreoElectronico,
            EstadoPersonal = personal.EstadoPersonal,
            EsRaiz = personal.EsRaiz,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EdicionPersonalViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var codigoAdministrador = identityUserId is null
            ? null
            : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);
        if (codigoAdministrador is null)
        {
            return Forbid();
        }
        var resultado = await _personalService.EditarAsync(modelo, codigoAdministrador.Value);
        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(modelo);
        }
        TempData["Mensaje"] = "El personal se editó correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = modelo.CodigoPersonal });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inactivar(InactivarPersonalViewModel modelo)
    {
        var personal = await _personalService.ObtenerDetalleAsync(modelo.CodigoPersonal);

        if (personal is null)
        {
            return NotFound();
        }

        await MarcarCuentaPropiaAsync(personal);

        if (!ModelState.IsValid)
        {
            return View(nameof(Detalle), personal);
        }
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var codigoAdministrador = identityUserId is null
            ? null
            : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);

        if (codigoAdministrador is null)
        {
            return Forbid();
        }
        var resultado = await _personalService.InactivarAsync(modelo, codigoAdministrador.Value);
        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(nameof(Detalle), personal);
        }

        TempData["Mensaje"] = "El integrante del personal se inactivó correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = modelo.CodigoPersonal });
    }

    private async Task MarcarCuentaPropiaAsync(PersonalDetalleViewModel personal)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return;
        }

        var codigoActual = await _personalService.ObtenerCodigoPersonalAsync(identityUserId);

        personal.EsCuentaPropia = codigoActual == personal.CodigoPersonal;
    }
}
