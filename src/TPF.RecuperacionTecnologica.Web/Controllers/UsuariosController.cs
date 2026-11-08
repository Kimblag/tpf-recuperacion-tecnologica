using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
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
    private readonly IPersonalService _personalService;

    public UsuariosController(
        IUsuarioService usuarioService,
        ICuentaService cuentaService,
        ISolicitudConsultaService solicitudConsultaService,
        IDonacionService donacionService,
        IPersonalService personalService)
    {
        _usuarioService = usuarioService;
        _cuentaService = cuentaService;
        _solicitudConsultaService = solicitudConsultaService;
        _donacionService = donacionService;
        _personalService = personalService;
    }

    [HttpGet]
    [Authorize(Policy = "SoloAdministrador")]
    public async Task<IActionResult> Index(TipoUsuario? tipo, EstadoUsuario? estado, string? orden)
    {
        return View(await _usuarioService.ListarAsync(tipo, estado, orden));
    }

    [HttpGet]
    [Authorize(Policy = "SoloAdministrador")]
    public async Task<IActionResult> Detalle(int id)
    {
        var usuario = await _usuarioService.ObtenerDetalleAsync(id);

        return usuario is null ? NotFound() : View(usuario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SoloAdministrador")]
    public async Task<IActionResult> Inactivar(InactivarUsuarioViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            TempData["MensajeError"] = "El motivo de inactivación es obligatorio.";
            return RedirectToAction(nameof(Detalle), new { id = modelo.NroUsuario });
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var codigoAdministrador = identityUserId is null
            ? null
            : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);

        if (codigoAdministrador is null)
        {
            return Forbid();
        }

        var resultado = await _usuarioService.InactivarAsync(modelo, codigoAdministrador.Value);

        if (!resultado.Succeeded)
        {
            TempData["MensajeError"] = resultado.Errors.First().Description;
            return RedirectToAction(nameof(Detalle), new { id = modelo.NroUsuario });
        }

        TempData["Mensaje"] = "El usuario se inactivó correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = modelo.NroUsuario });
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
    [Authorize(Policy = "Coordinacion")]
    public async Task<IActionResult> Corregir(int id)
    {
        var modelo = await _usuarioService.ObtenerCorreccionAsync(id);

        if (modelo is null)
        {
            TempData["AvisoInvitacion"] = "Esta cuenta ya no se puede corregir: la persona activó su cuenta o está dada de baja.";
            return RedirectToAction(nameof(Ficha), new { id });
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Coordinacion")]
    public async Task<IActionResult> Corregir(CorreccionUsuarioViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var codigoCoordinador = identityUserId is null
            ? null
            : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);

        if (codigoCoordinador is null)
        {
            return Forbid();
        }

        var resultado = await _usuarioService.CorregirAsync(modelo, codigoCoordinador.Value);

        if (!resultado.Exitoso)
        {
            ModelState.AddModelError(string.Empty, resultado.Mensaje!);
            return View(modelo);
        }

        if (resultado.CorreoCambio)
        {
            await _cuentaService.EnviarInvitacionAsync(
                resultado.IdentityUserId!,
                (idUsuario, token) => Url.Action("ActivarCuenta", "Cuenta", new { idUsuario, token })!,
                $"{Request.Scheme}://{Request.Host}");
        }

        TempData["Mensaje"] = resultado.CorreoCambio
            ? "Los datos se corrigieron y se envió una nueva invitación al correo actualizado."
            : "Los datos se corrigieron.";

        return RedirectToAction(nameof(Ficha), new { id = modelo.NroUsuario });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Coordinacion")]
    public async Task<IActionResult> ReenviarInvitacion(int id)
    {
        var resultado = await _cuentaService.ReenviarInvitacionAsync(
            id,
            (idUsuario, token) => Url.Action("ActivarCuenta", "Cuenta", new { idUsuario, token })!,
            $"{Request.Scheme}://{Request.Host}");

        if (resultado == ResultadoReenvioInvitacion.NoExiste)
        {
            return NotFound();
        }

        var mensaje = resultado switch
        {
            ResultadoReenvioInvitacion.Enviada => "Se envió una nueva invitación por correo. El enlace vence en 3 días.",
            ResultadoReenvioInvitacion.Espera => "Ya se envió una invitación hace menos de 2 minutos. Esperar un momento antes de volver a enviarla.",
            ResultadoReenvioInvitacion.YaActiva => "La cuenta ya eligió su contraseña, así que no hace falta otra invitación.",
            _ => "La cuenta está dada de baja y no se puede enviar una invitación."
        };

        // solo el envío correcto usa el mensaje verde; los demás se muestran como aviso en la ficha
        TempData[resultado == ResultadoReenvioInvitacion.Enviada ? "Mensaje" : "AvisoInvitacion"] = mensaje;

        return RedirectToAction(nameof(Ficha), new { id });
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

        TempData["Mensaje"] = "Tus datos se actualizaron correctamente.";
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
        TempData["Mensaje"] = "Tu cuenta fue dada de baja.";
        return RedirectToAction("Login", "Cuenta");
    }
}
