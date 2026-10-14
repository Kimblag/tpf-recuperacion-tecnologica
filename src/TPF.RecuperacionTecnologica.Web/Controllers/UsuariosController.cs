using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Usuarios;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize(Policy = "SoloAdministrador")]
public class UsuariosController : Controller
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(TipoUsuario? tipo, EstadoUsuario? estado)
    {
        return View(await _usuarioService.ListarAsync(tipo, estado));
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var usuario = await _usuarioService.ObtenerDetalleAsync(id);

        return usuario is null ? NotFound() : View(usuario);
    }
}
