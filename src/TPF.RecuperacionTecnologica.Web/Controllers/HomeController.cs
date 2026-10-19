using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Home;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Home;

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
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!User.HasClaim(c => c.Type == ClaimsSesion.TipoUsuario))
        {
            return User.IsInRole(nameof(RolPersonal.Tecnico)) ? await InicioTecnicoAsync(identityUserId) : View();
        }

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

    private async Task<IActionResult> InicioTecnicoAsync(string? identityUserId)
    {
        if (identityUserId is null)
        {
            return NotFound();
        }

        try
        {
            var inicio = await _inicioService.ObtenerInicioTecnicoAsync(identityUserId);

            return inicio is null ? NotFound() : View("InicioTecnico", inicio);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el inicio del Técnico");
            return View("InicioTecnico", new TecnicoInicioViewModel { ErrorAlCargar = true });
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
