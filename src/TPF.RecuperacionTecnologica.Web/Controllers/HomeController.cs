using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Home;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;
using TPF.RecuperacionTecnologica.Web.ViewModels.Home;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IInicioService _inicioService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IInicioService inicioService, ILogger<HomeController> logger)
    {
        _inicioService = inicioService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        if (!User.HasClaim(c => c.Type == ClaimsSesion.TipoUsuario))
        {
            return User.IsInRole(nameof(RolPersonal.Tecnico)) ? View("InicioTecnico", InicioTecnicoDeEjemplo()) : View();
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return NotFound();
        }

        try
        {
            var inicio = await _inicioService.ObtenerInicioUsuarioAsync(identityUserId);

            return inicio is null ? NotFound() : View("InicioUsuario", inicio);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el inicio del usuario");
            return View("InicioUsuarioError");
        }
    }

    private static TecnicoInicioViewModel InicioTecnicoDeEjemplo()
    {
        // TODO: reemplazar por el resumen real del servicio de inicio del Técnico
        return new TecnicoInicioViewModel
        {
            PendientesDeDiagnostico = 4,
            FechaPendienteMasAntiguo = new DateTime(2026, 9, 15),
            EnReparacion = 18,
            DiagnosticosDelMes = 9,
            EquiposDisponibles = 42,
            Pendientes =
            [
                Pendiente(130, TipoEquipo.Notebook, "Dell", "Latitude 3410", EstadoInicialDeclarado.ConFallas, new DateTime(2026, 9, 15)),
                Pendiente(127, TipoEquipo.Notebook, "Lenovo", "ThinkPad T440", EstadoInicialDeclarado.ConFallas, new DateTime(2026, 9, 17)),
                Pendiente(133, TipoEquipo.Monitor, "LG", "22MK430", EstadoInicialDeclarado.Funcional, new DateTime(2026, 9, 18)),
                Pendiente(136, TipoEquipo.Periferico, "Logitech", "K120", EstadoInicialDeclarado.Funcional, new DateTime(2026, 9, 19))
            ],
            ActividadReciente =
            [
                new() { Icono = "wrench", TextoAntes = "Diagnóstico ", Codigo = "DIA-001321", TextoDespues = " registrado", Detalle = "Equipo 000132 · 18/09/2026", EsExito = true },
                new() { Icono = "edit", TextoAntes = "Rectificación ", Codigo = "DIA-001312", TextoDespues = " guardada", Detalle = "Equipo 000131 · 08/08/2026" },
                new() { Icono = "wrench", TextoAntes = "Segundo diagnóstico ", Codigo = "DIA-001232", TextoDespues = " registrado", Detalle = "Equipo 000123 · 31/08/2026", EsExito = true }
            ]
        };
    }

    private static EquipoFilaViewModel Pendiente(
        int nroEquipo, TipoEquipo tipo, string marca, string modelo, EstadoInicialDeclarado condicion, DateTime fecha) => new()
        {
            NroEquipo = nroEquipo,
            TipoEquipo = tipo,
            Marca = marca,
            Modelo = modelo,
            EstadoInicialDeclarado = condicion,
            FechaRegistro = fecha,
            EstadoEquipo = EstadoEquipo.PendienteRecepcion
        };

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
