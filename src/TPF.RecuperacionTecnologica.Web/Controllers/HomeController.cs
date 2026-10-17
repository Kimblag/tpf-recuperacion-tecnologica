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
    private readonly ILogger<HomeController> _logger;

    public HomeController(IInicioService inicioService, ILogger<HomeController> logger)
    {
        _inicioService = inicioService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        if (!User.HasClaim(c => c.Type == ClaimsSesion.TipoUsuario))
        {
            return View();
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return NotFound();
        }

        try
        {
            var inicio = await _inicioService.ObtenerInicioUsuarioAsync(identityUserId);

            return inicio is null ? NotFound() : View("InicioUsuario", inicio);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el inicio del usuario");
            return View("InicioUsuarioError");
        }
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
