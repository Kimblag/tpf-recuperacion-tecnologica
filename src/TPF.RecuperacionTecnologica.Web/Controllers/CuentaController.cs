using Microsoft.AspNetCore.Mvc;
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

        TempData["Mensaje"] = "Registro exitoso.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
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

            case ResultadoInicioSesion.CuentaBloqueada:
                ModelState.AddModelError(string.Empty,
                    "La cuenta está bloqueada temporalmente por varios intentos fallidos. Intente nuevamente más tarde.");
                break;

            case ResultadoInicioSesion.CuentaInactiva:
                ModelState.AddModelError(string.Empty,
                    "Su cuenta no está habilitada para ingresar. Comuníquese con la organización.");
                break;

            default:
                ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                break;
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _cuentaService.CerrarSesionAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccesoDenegado()
    {
        return View();
    }
}