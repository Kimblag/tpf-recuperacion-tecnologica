using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.ViewModels.Parametros;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

// TODO: restringir a la política SoloAdministrador
[Authorize]
public class ParametrosController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(DatosDeEjemplo());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(ParametrosViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        // TODO: guardar los parámetros con el servicio y registrar la auditoría
        ModelState.AddModelError(string.Empty, "La modificación de parámetros todavía no está implementada.");
        return View(modelo);
    }

    // TODO: reemplazar estos datos de ejemplo por los parámetros reales guardados
    private static ParametrosViewModel DatosDeEjemplo() => new()
    {
        NombreOrganizacion = "Recuperación Tecnológica",
        ZonaCobertura = "Zona Sur del Gran Buenos Aires",
        DiasLimiteRetiro = 10,
        CodigoAdministrador = 1,
        FechaUltimaModificacion = new DateTime(2026, 9, 18)
    };
}
