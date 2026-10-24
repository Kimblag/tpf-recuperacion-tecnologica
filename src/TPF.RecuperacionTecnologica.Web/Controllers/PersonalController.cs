using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Personal;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize(Policy = "SoloAdministrador")]
public class PersonalController : Controller
{
    private readonly IPersonalService _personalService;

    public PersonalController(IPersonalService personalService)
    {
        _personalService = personalService;
    }

    // TODO: reemplazar estos datos de ejemplo por los reales del servicio de personal interno
    private static readonly IReadOnlyList<PersonalDetalleViewModel> DatosDeEjemplo =
    [
        Ejemplo(1, "27884310", "Matías", "Correa", "11 5550-1201", "matias.correa@example.org", RolPersonal.Administrador, EstadoPersonal.Activo),
        Ejemplo(7, "30115642", "Sofía", "Gutiérrez", "11 5550-1207", "sofia.gutierrez@example.org", RolPersonal.Coordinador, EstadoPersonal.Activo),
        Ejemplo(9, "31660298", "Ramiro", "Ortega", "11 5550-1209", "ramiro.ortega@example.org", RolPersonal.Coordinador, EstadoPersonal.Activo),
        Ejemplo(12, "33902117", "Tomás", "Ibáñez", "11 5550-1212", "tomas.ibanez@example.org", RolPersonal.Tecnico, EstadoPersonal.Activo),
        Ejemplo(14, "35447803", "Agustina", "Luna", "11 5550-1214", "agustina.luna@example.org", RolPersonal.Tecnico, EstadoPersonal.Activo),
        Ejemplo(15, "32019775", "Federico", "Paz", "11 5550-1215", "federico.paz@example.org", RolPersonal.Tecnico, EstadoPersonal.Inactivo)
    ];

    [HttpGet]
    public async Task<IActionResult> Index(string? busqueda, RolPersonal? rol, EstadoPersonal? estado)
    {
        return View(await _personalService.ListarAsync(busqueda, rol, estado));
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

        return personal is null ? NotFound() : View(personal);
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var personal = await _personalService.ObtenerDetalleAsync(id);

        if (personal is null)
        {
            return NotFound();
        }

        return View(new EdicionPersonalViewModel
        {
            CodigoPersonal = personal.CodigoPersonal,
            Dni = personal.Dni,
            Nombre = personal.Nombre,
            Apellido = personal.Apellido,
            Telefono = personal.Telefono,
            RolAsignado = personal.RolAsignado,
            CorreoElectronico = personal.CorreoElectronico,
            EstadoPersonal = personal.EstadoPersonal
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EdicionPersonalViewModel modelo)
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

    private static PersonalDetalleViewModel Ejemplo(
        int codigo, string dni, string nombre, string apellido, string telefono,
        string correo, RolPersonal rol, EstadoPersonal estado) => new()
        {
            CodigoPersonal = codigo,
            Dni = dni,
            Nombre = nombre,
            Apellido = apellido,
            Telefono = telefono,
            CorreoElectronico = correo,
            RolAsignado = rol,
            EstadoPersonal = estado
        };
}
