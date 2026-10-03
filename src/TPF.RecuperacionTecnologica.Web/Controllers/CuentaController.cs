using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Services.Usuarios;
using TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class CuentaController : Controller
{
    private readonly IUsuarioService _usuarioService;


    public CuentaController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
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
}
