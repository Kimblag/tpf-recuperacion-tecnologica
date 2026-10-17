using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Home;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IInicioService _inicioService;

    public HomeController(IInicioService inicioService)
    {
        _inicioService = inicioService;
    }

    public async Task<IActionResult> Index()
    {
        if (!User.HasClaim(c => c.Type == ClaimsSesion.TipoUsuario))
        {
            return View();
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var inicio = identityUserId is null
            ? null
            : await _inicioService.ObtenerInicioUsuarioAsync(identityUserId);

        return inicio is null ? NotFound() : View("InicioUsuario", inicio);
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
