using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Usuarios;
using TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class UsuariosController : Controller
{
    private readonly IUsuarioService _usuarioService;
    private readonly ICuentaService _cuentaService;

    public UsuariosController(IUsuarioService usuarioService, ICuentaService cuentaService)
    {
        _usuarioService = usuarioService;
        _cuentaService = cuentaService;
    }

    [HttpGet]
    [Authorize(Policy = "SoloAdministrador")]
    public async Task<IActionResult> Index(TipoUsuario? tipo, EstadoUsuario? estado)
    {
        return View(await _usuarioService.ListarAsync(tipo, estado));
    }

    [HttpGet]
    [Authorize(Policy = "SoloAdministrador")]
    public async Task<IActionResult> Detalle(int id)
    {
        var usuario = await _usuarioService.ObtenerDetalleAsync(id);

        return usuario is null ? NotFound() : View(usuario);
    }

    // el Usuario edita sus propios datos: se identifica por la sesión, no por un id en la URL
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Editar()
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var modelo = identityUserId is null ? null : await _usuarioService.ObtenerEdicionAsync(identityUserId);

        return modelo is null ? NotFound() : View(modelo);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EdicionUsuarioViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return NotFound();
        }

        var (resultado, correoCambiado) = await _usuarioService.EditarAsync(identityUserId, modelo);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(modelo);
        }

        // al cambiar el correo la sesión deja de ser válida, así que se cierra y se pide entrar de nuevo
        if (correoCambiado)
        {
            await _cuentaService.CerrarSesionAsync(identityUserId);
            TempData["Mensaje"] = "Sus datos se actualizaron. Inicie sesión con su nuevo correo.";
            return RedirectToAction("Login", "Cuenta");
        }

        TempData["Mensaje"] = "Sus datos se actualizaron correctamente.";
        return RedirectToAction(nameof(Editar));
    }
}
