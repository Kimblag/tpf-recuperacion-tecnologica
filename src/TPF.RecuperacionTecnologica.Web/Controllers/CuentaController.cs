
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Account;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class CuentaController : Controller
{
private readonly SignInManager<IdentityUser> _signInManager;
private readonly UserManager<IdentityUser> _userManager;
private readonly ApplicationDbContext _context;

public CuentaController(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    ApplicationDbContext context)
{
    _signInManager = signInManager;
    _userManager = userManager;
    _context = context;
}

[HttpGet]
public IActionResult Login()
{
    return View();
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    var identityUser = await _userManager.FindByEmailAsync(
        model.CorreoElectronico);

    if (identityUser == null)
    {
        ModelState.AddModelError(
            string.Empty,
            "Correo o contraseña incorrectos.");

        return View(model);
    }

    var personal = await _context.PersonalInterno
        .SingleOrDefaultAsync(p =>
            p.IdentityUserId == identityUser.Id);

    var usuario = await _context.Usuarios
        .SingleOrDefaultAsync(u =>
            u.IdentityUserId == identityUser.Id);

    if (personal != null)
    {
        if (personal.EstadoPersonal != EstadoPersonal.Activo)
        {
            ModelState.AddModelError(
                string.Empty,
                "La cuenta está inactiva.");

            return View(model);
        }
    }
    else if (usuario == null ||
             usuario.EstadoUsuario != EstadoUsuario.Activo)
    {
        ModelState.AddModelError(
            string.Empty,
            "La cuenta está inactiva o no está habilitada.");

        return View(model);
    }

    var resultado = await _signInManager.PasswordSignInAsync(
        identityUser.UserName!,
        model.Password,
        model.RecordarSesion,
        lockoutOnFailure: true);

    if (resultado.IsLockedOut)
    {
        ModelState.AddModelError(
            string.Empty,
            "La cuenta está temporalmente bloqueada.");

        return View(model);
    }

    if (!resultado.Succeeded)
    {
        ModelState.AddModelError(
            string.Empty,
            "Correo o contraseña incorrectos.");

        return View(model);
    }

    // Pendiente de reemplazar cuando estén definidos los paneles.
    return RedirectToAction("Index", "Home");
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Logout()
{
    await _signInManager.SignOutAsync();

    return RedirectToAction("Index", "Home");
}

[HttpGet]
public IActionResult AccesoDenegado()
{
    return View();
}

}