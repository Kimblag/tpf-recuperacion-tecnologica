using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Personal;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

// TODO: restringir a la política SoloAdministrador
[Authorize]
public class PersonalController : Controller
{
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
    public IActionResult Index()
    {
        return View(new PersonalListadoViewModel
        {
            Filas = DatosDeEjemplo,
            Total = DatosDeEjemplo.Count
        });
    }

    [HttpGet]
    public IActionResult Alta()
    {
        return View(new AltaPersonalViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Alta(AltaPersonalViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        // TODO: AltaAsync 
        ModelState.AddModelError(string.Empty, "El alta de personal todavía no está implementada.");
        return View(modelo);
    }

    [HttpGet]
    public IActionResult Detalle(int id)
    {
        var personal = DatosDeEjemplo.FirstOrDefault(p => p.CodigoPersonal == id);

        return personal is null ? NotFound() : View(personal);
    }

    [HttpGet]
    public IActionResult Editar(int id)
    {
        var personal = DatosDeEjemplo.FirstOrDefault(p => p.CodigoPersonal == id);

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
    public IActionResult Editar(EdicionPersonalViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        // TODO: EditarAsync
        ModelState.AddModelError(string.Empty, "La edición de personal todavía no está implementada.");
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Inactivar(InactivarPersonalViewModel modelo)
    {
        var personal = DatosDeEjemplo.FirstOrDefault(p => p.CodigoPersonal == modelo.CodigoPersonal);

        if (personal is null)
        {
            return NotFound();
        }

        // TODO:InactivarAsync 
        if (ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "La inactivación de personal todavía no está implementada.");
        }

        return View(nameof(Detalle), personal);
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
