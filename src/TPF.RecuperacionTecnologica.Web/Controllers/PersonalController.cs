using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.ViewModels.Personal;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize(Policy = "SoloAdministrador")]
public class PersonalController : Controller
{
    private const int CantidadActividad = 10;

    private readonly IPersonalService _personalService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ICuentaService _cuentaService;

    public PersonalController(IPersonalService personalService, IAuditoriaService auditoriaService, ICuentaService cuentaService)
    {
        _personalService = personalService;
        _auditoriaService = auditoriaService;
        _cuentaService = cuentaService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? busqueda, RolPersonal? rol, EstadoPersonal? estado, string? acceso, string? orden, int pagina = 1)
    {
        var listado = await _personalService.ListarAsync(busqueda, rol, estado, acceso, orden, pagina, !ModelState.IsValid);
        listado.CodigoActual = await ObtenerCodigoActualAsync();

        return View(listado);
    }

    [HttpGet]
    public IActionResult Alta()
    {
        return View(new AltaPersonalViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Alta(AltaPersonalViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var codigoAdministrador = identityUserId is null
            ? null
            : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);

        if (codigoAdministrador is null)
        {
            return Forbid();
        }

        var resultado = await _personalService.AltaAsync(modelo, codigoAdministrador.Value);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(modelo);
        }

        TempData["Mensaje"] = "El personal se dio de alta correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var personal = await _personalService.ObtenerDetalleAsync(id);

        if (personal is null)
        {
            return NotFound();
        }

        await PrepararDetalleAsync(personal);

        return View(personal);
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var personal = await _personalService.ObtenerDetalleAsync(id);

        if (personal is null)
        {
            return NotFound();
        }

        if (personal.EsRaiz)
        {
            return RedirectToAction(nameof(Detalle), new { id });
        }

        if (await ObtenerCodigoActualAsync() == id)
        {
            TempData["MensajeError"] = "No se puede editar la propia cuenta. Otro Administrador debe hacerlo.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        return View(new EdicionPersonalViewModel
        {
            CodigoPersonal = personal.CodigoPersonal,
            RowVersion = personal.RowVersion,
            Dni = personal.Dni,
            Nombre = personal.Nombre,
            Apellido = personal.Apellido,
            Telefono = personal.Telefono,
            RolAsignado = personal.RolAsignado,
            CorreoElectronico = personal.CorreoElectronico,
            EstadoPersonal = personal.EstadoPersonal,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EdicionPersonalViewModel modelo)
    {
        if (await ObtenerCodigoActualAsync() == modelo.CodigoPersonal)
        {
            TempData["MensajeError"] = "No se puede editar la propia cuenta. Otro Administrador debe hacerlo.";
            return RedirectToAction(nameof(Detalle), new { id = modelo.CodigoPersonal });
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var codigoAdministrador = identityUserId is null
            ? null
            : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);
        if (codigoAdministrador is null)
        {
            return Forbid();
        }
        var resultado = await _personalService.EditarAsync(modelo, codigoAdministrador.Value);
        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(modelo);
        }
        TempData["Mensaje"] = "El personal se editó correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = modelo.CodigoPersonal });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inactivar(InactivarPersonalViewModel modelo)
    {
        var personal = await _personalService.ObtenerDetalleAsync(modelo.CodigoPersonal);

        if (personal is null)
        {
            return NotFound();
        }

        await PrepararDetalleAsync(personal);

        if (!ModelState.IsValid)
        {
            return View(nameof(Detalle), personal);
        }
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var codigoAdministrador = identityUserId is null
            ? null
            : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);

        if (codigoAdministrador is null)
        {
            return Forbid();
        }
        var resultado = await _personalService.InactivarAsync(modelo, codigoAdministrador.Value);
        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(nameof(Detalle), personal);
        }

        TempData["Mensaje"] = "El integrante del personal se inactivó correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = modelo.CodigoPersonal });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id)
    {
        if (await _personalService.ObtenerDetalleAsync(id) is null)
        {
            return NotFound();
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var codigoAdministrador = identityUserId is null
            ? null
            : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);

        if (codigoAdministrador is null)
        {
            return Forbid();
        }

        var resultado = await _personalService.ReactivarAsync(id, codigoAdministrador.Value);

        if (!resultado.Succeeded)
        {
            TempData["MensajeError"] = resultado.Errors.First().Description;
            return RedirectToAction(nameof(Detalle), new { id });
        }

        TempData["Mensaje"] = "El integrante del personal se reactivó correctamente.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desbloquear(int id)
    {
        var codigoAdministrador = await ObtenerCodigoActualAsync();

        if (codigoAdministrador is null)
        {
            return Forbid();
        }

        var resultado = await _personalService.DesbloquearAsync(id, codigoAdministrador.Value);

        if (!resultado.Succeeded && resultado.Errors.First().Code == "PersonalNoEncontrado")
        {
            return NotFound();
        }

        TempData[resultado.Succeeded ? "Mensaje" : "MensajeError"] = resultado.Succeeded
            ? "La cuenta se desbloqueó. La persona ya puede iniciar sesión."
            : resultado.Errors.First().Description;

        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarAcceso(int id)
    {
        var personal = await _personalService.ObtenerDetalleAsync(id);

        if (personal is null)
        {
            return NotFound();
        }

        var codigoAdministrador = await ObtenerCodigoActualAsync();

        if (codigoAdministrador is null)
        {
            return Forbid();
        }

        if (codigoAdministrador == id)
        {
            TempData["MensajeError"] = "Para cambiar tu propia contraseña usá «Cambiar contraseña» en Mi cuenta.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        var resultado = await _cuentaService.ReenviarAccesoPersonalAsync(
            id,
            (idUsuario, token) => Url.Action("RestablecerContrasena", "Cuenta", new { idUsuario, token })!,
            $"{Request.Scheme}://{Request.Host}");

        if (resultado == ResultadoReenvioInvitacion.NoExiste)
        {
            return NotFound();
        }

        if (resultado == ResultadoReenvioInvitacion.Enviada)
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.EnvioAccesoPersonal, codigoAdministrador, nameof(Models.Personal), id,
                "Envío de enlace para restablecer la contraseña");

            TempData["Mensaje"] = "Se envió al correo registrado un enlace para restablecer la contraseña. Vence en 1 hora.";
        }
        else
        {
            TempData["MensajeError"] = resultado == ResultadoReenvioInvitacion.Espera
                ? "Ya se envió un correo hace menos de 2 minutos. Esperá un momento antes de volver a enviarlo."
                : "La cuenta está inactiva: no se puede enviar el acceso.";
        }

        return RedirectToAction(nameof(Detalle), new { id });
    }

    private async Task<int?> ObtenerCodigoActualAsync()
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return identityUserId is null ? null : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);
    }

    private async Task PrepararDetalleAsync(PersonalDetalleViewModel personal)
    {
        var actividad = await _auditoriaService.ConsultarAsync(
            null, null, null, personal.CodigoPersonal, null, null, 1, CantidadActividad);

        personal.Actividad = actividad.Elementos;
        personal.TotalActividad = actividad.Total;

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return;
        }

        var codigoActual = await _personalService.ObtenerCodigoPersonalAsync(identityUserId);

        personal.EsCuentaPropia = codigoActual == personal.CodigoPersonal;
    }
}