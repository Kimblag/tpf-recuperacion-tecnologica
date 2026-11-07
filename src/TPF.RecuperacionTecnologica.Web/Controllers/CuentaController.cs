using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.Services.Usuarios;
using TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class CuentaController : Controller
{
    private readonly IUsuarioService _usuarioService;
    private readonly ICuentaService _cuentaService;
    private readonly IPersonalService _personalService;

    public CuentaController(IUsuarioService usuarioService, ICuentaService cuentaService, IPersonalService personalService)
    {
        _usuarioService = usuarioService;
        _cuentaService = cuentaService;
        _personalService = personalService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Registrar()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegistroUsuarioViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _cuentaService.CerrarSesionAsync(identityUserId);

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RecuperarContrasena()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RecuperarContrasenaViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> RecuperarContrasena(RecuperarContrasenaViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        await _cuentaService.SolicitarRestablecimientoAsync(
            modelo.CorreoElectronico,
            (idUsuario, token) => Url.Action(nameof(RestablecerContrasena), "Cuenta", new { idUsuario, token })!,
            $"{Request.Scheme}://{Request.Host}");

        TempData["Mensaje"] = "Si el correo está registrado, le enviamos las instrucciones para restablecer su contraseña.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult RestablecerContrasena(string? idUsuario = null, string? token = null)
    {
        if (string.IsNullOrEmpty(idUsuario) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction(nameof(RecuperarContrasena));
        }

        return View(new RestablecerContrasenaViewModel { IdUsuario = idUsuario, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestablecerContrasena(RestablecerContrasenaViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            // si faltan el identificador o el token, el enlace es inválido y debe solicitar uno nuevo
            ViewData["EnlaceInvalido"] =
                (ModelState[nameof(modelo.IdUsuario)]?.Errors.Count ?? 0) > 0 ||
                (ModelState[nameof(modelo.Token)]?.Errors.Count ?? 0) > 0;

            return View(modelo);
        }

        var resultado = await _cuentaService.RestablecerContrasenaAsync(modelo.IdUsuario, modelo.Token, modelo.Password);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewData["EnlaceInvalido"] = resultado.Errors.Any(e => e.Code == "InvalidToken");

            return View(modelo);
        }

        TempData["Mensaje"] = "Contraseña actualizada correctamente. Ya puede iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ActivarCuenta(string? idUsuario = null, string? token = null)
    {
        // se revisa el enlace al abrir la pantalla, para no pedir una contraseña que después no se va a poder guardar
        if (string.IsNullOrEmpty(idUsuario) || string.IsNullOrEmpty(token)
            || !await _cuentaService.EsEnlaceActivacionValidoAsync(idUsuario, token))
        {
            ViewData["EnlaceInvalido"] = true;
            return View(new ActivarCuentaViewModel());
        }

        return View(new ActivarCuentaViewModel { IdUsuario = idUsuario, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> ActivarCuenta(ActivarCuentaViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            // si faltan el identificador o el token, el enlace no sirve y hay que pedir otro
            ViewData["EnlaceInvalido"] =
                (ModelState[nameof(modelo.IdUsuario)]?.Errors.Count ?? 0) > 0 ||
                (ModelState[nameof(modelo.Token)]?.Errors.Count ?? 0) > 0;

            return View(modelo);
        }

        var resultado = await _cuentaService.ActivarCuentaAsync(modelo.IdUsuario, modelo.Token, modelo.Password);

        if (!resultado.Succeeded)
        {
            var enlaceInvalido = resultado.Errors.Any(e => e.Code == "InvalidToken");

            // el aviso de enlace vencido ya explica qué hacer, así que no se repite como error
            if (!enlaceInvalido)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["EnlaceInvalido"] = enlaceInvalido;

            return View(modelo);
        }

        TempData["Mensaje"] = "La cuenta quedó activada. Ya se puede iniciar sesión con el correo y la contraseña elegida.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public async Task<IActionResult> MiCuenta()
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        var cuenta = await _personalService.ObtenerMiCuentaAsync(identityUserId);

        return cuenta is null ? NotFound() : View(cuenta);
    }

    [HttpGet]
    [Authorize]
    public IActionResult CambiarContrasena()
    {
        return View(new CambiarContrasenaViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> CambiarContrasena(CambiarContrasenaViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        var resultado = await _cuentaService.CambiarContrasenaAsync(identityUserId, modelo.ContrasenaActual, modelo.Password);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                // la contraseña actual incorrecta se marca en su campo; las reglas de la nueva, en el de la nueva
                var campo = error.Code == "PasswordMismatch"
                    ? nameof(modelo.ContrasenaActual)
                    : error.Code.StartsWith("Password") ? nameof(modelo.Password) : string.Empty;

                ModelState.AddModelError(campo, error.Description);
            }

            return View(modelo);
        }

        TempData["Mensaje"] = "La contraseña se cambió correctamente. Las demás sesiones abiertas se cerraron.";
        return RedirigirACuenta();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccesoDenegado()
    {
        return View();
    }

    /// <summary>
    /// Vuelve a la pantalla de cuenta de cada uno: el usuario externo a su perfil y el personal a Mi cuenta.
    /// </summary>
    private IActionResult RedirigirACuenta()
    {
        return User.HasClaim(c => c.Type == ClaimsSesion.TipoUsuario)
            ? RedirectToAction("Perfil", "Usuarios")
            : RedirectToAction(nameof(MiCuenta));
    }
}