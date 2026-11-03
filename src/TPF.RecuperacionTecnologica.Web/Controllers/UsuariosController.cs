using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.Services.Solicitudes;
using TPF.RecuperacionTecnologica.Web.Services.Usuarios;
using TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class UsuariosController : Controller
{
    // la ficha muestra las últimas solicitudes y donaciones de la persona
    private const int CantidadEnFicha = 10;

    private readonly IUsuarioService _usuarioService;
    private readonly ICuentaService _cuentaService;
    private readonly ISolicitudConsultaService _solicitudConsultaService;
    private readonly IDonacionService _donacionService;

    public UsuariosController(
        IUsuarioService usuarioService,
        ICuentaService cuentaService,
        ISolicitudConsultaService solicitudConsultaService,
        IDonacionService donacionService)
    {
        _usuarioService = usuarioService;
        _cuentaService = cuentaService;
        _solicitudConsultaService = solicitudConsultaService;
        _donacionService = donacionService;
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

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public async Task<IActionResult> Ficha(int id)
    {
        var ficha = await _usuarioService.ObtenerFichaAsync(id);

        if (ficha is null)
        {
            return NotFound();
        }

        var solicitudes = await _solicitudConsultaService.ListarTodasAsync(
            null, null, null, null, 1, CantidadEnFicha, nroUsuario: id);
        var donaciones = await _donacionService.ListarTodasAsync(
            null, null, null, 1, CantidadEnFicha, nroUsuario: id);

        ficha.Solicitudes = solicitudes.Elementos;
        ficha.Donaciones = donaciones.Elementos;

        return View(ficha);
    }


    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Perfil()
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var modelo = identityUserId is null ? null : await _usuarioService.ObtenerEdicionAsync(identityUserId);

        return modelo is null ? NotFound() : View(modelo);
    }


    // el Usuario edita sus propios datos: se identifica por la sesión, no por un id en la URL
    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Editar()
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var modelo = identityUserId is null ? null : await _usuarioService.ObtenerEdicionAsync(identityUserId);

        return modelo is null ? NotFound() : View(modelo);
    }

    [HttpPost]
    [Authorize(Policy = "UsuarioExterno")]
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

        var resultado = await _usuarioService.EditarAsync(identityUserId, modelo);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(modelo);
        }

        await _cuentaService.RefrescarSesionAsync(identityUserId);

        TempData["Mensaje"] = "Sus datos se actualizaron correctamente.";
        return RedirectToAction(nameof(Perfil));
    }

    // el Usuario da de baja su propia cuenta: se identifica por la sesión, no por un id en la URL
    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Baja()
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var modelo = identityUserId is null ? null : await _usuarioService.ObtenerEdicionAsync(identityUserId);

        return modelo is null ? NotFound() : View(new BajaUsuarioViewModel());
    }

    [HttpPost]
    [Authorize(Policy = "UsuarioExterno")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Baja(BajaUsuarioViewModel modelo)
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

        var resultado = await _usuarioService.DarDeBajaAsync(identityUserId, modelo);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(modelo);
        }

        await _cuentaService.CerrarSesionAsync(identityUserId);
        TempData["Mensaje"] = "Su cuenta fue dada de baja.";
        return RedirectToAction("Login", "Cuenta");
    }
}
