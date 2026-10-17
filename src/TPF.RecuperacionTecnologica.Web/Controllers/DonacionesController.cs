using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class DonacionesController : Controller
{
    private readonly IDonacionService _donacionService;

    public DonacionesController(IDonacionService donacionService)
    {
        _donacionService = donacionService;
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public IActionResult Index(MisDonacionesViewModel filtros)
    {
        // TODO: reemplazar estos datos de ejemplo por las donaciones reales del usuario, con filtros y paginación
        filtros.Filas =
        [
            Fila(127, TipoEquipo.Notebook, "Lenovo", "ThinkPad T440", EstadoInicialDeclarado.ConFallas, new DateTime(2026, 9, 17), EstadoEquipo.PendienteRecepcion),
            Fila(132, TipoEquipo.PcEscritorio, "HP", "ProDesk 400", EstadoInicialDeclarado.Funcional, new DateTime(2026, 9, 16), EstadoEquipo.EnReparacion),
            Fila(125, TipoEquipo.Notebook, "Acer", "Aspire 5", EstadoInicialDeclarado.Funcional, new DateTime(2026, 8, 25), EstadoEquipo.Asignado),
            Fila(123, TipoEquipo.Notebook, "Dell", "Latitude E6440", EstadoInicialDeclarado.Funcional, new DateTime(2026, 8, 24), EstadoEquipo.Disponible),
            Fila(129, TipoEquipo.Monitor, "Samsung", "S22F350", EstadoInicialDeclarado.Funcional, new DateTime(2026, 8, 8), EstadoEquipo.Entregado),
            Fila(131, TipoEquipo.Notebook, "Toshiba", "Satellite C55", EstadoInicialDeclarado.ParaRepuestos, new DateTime(2026, 8, 5), EstadoEquipo.Baja),
            Fila(134, TipoEquipo.Celular, "Samsung", "Galaxy A10", EstadoInicialDeclarado.Funcional, new DateTime(2026, 9, 18), EstadoEquipo.Cancelado)
        ];
        filtros.Total = filtros.Filas.Count;

        return View(filtros);
    }

    private static DonacionFilaViewModel Fila(
        int nroEquipo, TipoEquipo tipo, string marca, string modelo,
        EstadoInicialDeclarado condicion, DateTime fecha, EstadoEquipo estado) => new()
        {
            NroEquipo = nroEquipo,
            TipoEquipo = tipo,
            Marca = marca,
            Modelo = modelo,
            EstadoInicialDeclarado = condicion,
            FechaRegistro = fecha,
            EstadoEquipo = estado
        };

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public IActionResult Registrar()
    {
        return View(new RegistroDonacionViewModel());
    }

    // el donante se identifica por la sesión, no por un dato del formulario.
    // el parámetro no se llama "modelo" porque el ViewModel tiene la propiedad Modelo y confundiría al model binding
    [HttpPost]
    [Authorize(Policy = "UsuarioExterno")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegistroDonacionViewModel donacion)
    {
        if (!ModelState.IsValid)
        {
            return View(donacion);
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var nroEquipo = identityUserId is null ? null : await _donacionService.RegistrarAsync(identityUserId, donacion);

        return nroEquipo is null
            ? NotFound()
            : RedirectToAction(nameof(Confirmacion), new { id = nroEquipo });
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public IActionResult Confirmacion(int id)
    {
        return View(id);
    }
}
