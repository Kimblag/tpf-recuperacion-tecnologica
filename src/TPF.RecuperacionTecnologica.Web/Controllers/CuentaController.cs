using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Usuarios;
using TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class CuentaController : Controller
{
    private readonly IUsuarioService _usuarioService;
    private readonly ICuentaService _cuentaService;

    public CuentaController(IUsuarioService usuarioService, ICuentaService cuentaService)
    {
        _usuarioService = usuarioService;
        _cuentaService = cuentaService;
    }

    [HttpGet]
    public IActionResult Registrar()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegistroUsuarioViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegistroUsuarioViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado = await _usuarioService.RegistrarAsync(modelo);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(modelo);
        }

        TempData["Mensaje"] = "Cuenta creada correctamente. Ya puede iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var resultado = await _cuentaService.IniciarSesionAsync(
            modelo.CorreoElectronico, modelo.Password, modelo.RecordarSesion);

        switch (resultado)
        {
            case ResultadoInicioSesion.Exitoso:
                // pendiente: redirigir al panel de cada rol cuando estén definidos
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");

            case ResultadoInicioSesion.CuentaInactiva:
                ModelState.AddModelError(string.Empty,
                    "Su cuenta no está habilitada para ingresar. Comuníquese con la organización.");
                break;

            default:
                // credenciales incorrectas y cuenta bloqueada muestran el mismo mensaje
                // para no revelar qué correos existen
                ModelState.AddModelError(string.Empty,
                    "No se pudo iniciar sesión. Verifique sus datos o intente nuevamente más tarde.");
                break;
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _cuentaService.CerrarSesionAsync(identityUserId);

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccesoDenegado()
    {
        return View();
    }
}