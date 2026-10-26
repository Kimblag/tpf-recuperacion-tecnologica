using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Services.Configuracion;
using TPF.RecuperacionTecnologica.Web.ViewModels.Parametros;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize(Policy = "SoloAdministrador")]
public class ParametrosController : Controller
{
    private readonly IConfiguracionService _configuracionService;

    public ParametrosController(IConfiguracionService configuracionService)
    {
        _configuracionService = configuracionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var configuracion = await _configuracionService.ObtenerAsync();

        if (configuracion is null)
        {
            return NotFound();
        }

        return View(new ParametrosViewModel
        {
            NombreOrganizacion = configuracion.NombreOrganizacion,
            ZonaCobertura = configuracion.ZonaCobertura,
            DiasLimiteRetiro = configuracion.DiasLimiteRetiro,
            MaximoSolicitudesPendientes = configuracion.MaximoSolicitudesPendientes,
            DiasAvisoVencimiento = configuracion.DiasAvisoVencimiento,
            CodigoAdministrador = configuracion.CodigoAdministrador,
            FechaUltimaModificacion = configuracion.FechaUltimaModificacion,
            RowVersion = configuracion.RowVersion
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        [Bind("NombreOrganizacion,ZonaCobertura,DiasLimiteRetiro,MaximoSolicitudesPendientes,DiasAvisoVencimiento,RowVersion")] ParametrosViewModel modelo)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        if (ModelState.IsValid)
        {
            var resultado = await _configuracionService.EditarAsync(identityUserId, modelo);

            if (resultado.Succeeded)
            {
                TempData["Mensaje"] = "Los parámetros se guardaron correctamente.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // la última modificación no viaja en el formulario: se vuelve a leer la guardada
        var actual = await _configuracionService.ObtenerAsync();
        modelo.CodigoAdministrador = actual?.CodigoAdministrador;
        modelo.FechaUltimaModificacion = actual?.FechaUltimaModificacion;

        return View(modelo);
    }
}
