using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Services.Home;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize(Policy = "SoloAdministrador")]
public class AdministradorController : Controller
{
    private readonly IInicioService _inicioService;

    public AdministradorController(IInicioService inicioService)
    {
        _inicioService = inicioService;
    }

    [HttpGet]
    public async Task<IActionResult> Inicio()
    {
        return View(await _inicioService.ObtenerInicioAdministradorAsync());
    }
}