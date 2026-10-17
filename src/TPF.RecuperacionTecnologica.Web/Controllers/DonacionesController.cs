using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class DonacionesController : Controller
{
    private readonly IDonacionService _donacionService;

    // TODO: reemplazar estos datos de ejemplo por las donaciones reales del usuario
    private static readonly IReadOnlyList<DonacionDetalleViewModel> DatosDeEjemplo =
    [
        Donacion(127, TipoEquipo.Notebook, "Lenovo", "ThinkPad T440", EstadoInicialDeclarado.ConFallas, new DateTime(2026, 9, 17), EstadoEquipo.PendienteRecepcion,
            "La pantalla tiene una línea vertical.", "Intel Core i5", "8 GB", "256 GB SSD", "14 pulgadas", "Dura 2 horas"),
        Donacion(132, TipoEquipo.PcEscritorio, "HP", "ProDesk 400", EstadoInicialDeclarado.Funcional, new DateTime(2026, 9, 16), EstadoEquipo.EnReparacion,
            "Sin mouse ni teclado.", "Intel Core i3", "4 GB", "500 GB HDD"),
        Donacion(125, TipoEquipo.Notebook, "Acer", "Aspire 5", EstadoInicialDeclarado.Funcional, new DateTime(2026, 8, 25), EstadoEquipo.Asignado,
            "Batería con poca autonomía.", "AMD Ryzen 5", "8 GB", "512 GB SSD", "15,6 pulgadas"),
        Donacion(123, TipoEquipo.Notebook, "Dell", "Latitude E6440", EstadoInicialDeclarado.Funcional, new DateTime(2026, 8, 24), EstadoEquipo.Disponible,
            "Incluye cargador original. Pantalla sin rayones.", "Intel Core i5", "8 GB", "240 GB SSD", "14 pulgadas"),
        Donacion(129, TipoEquipo.Monitor, "Samsung", "S22F350", EstadoInicialDeclarado.Funcional, new DateTime(2026, 8, 8), EstadoEquipo.Entregado,
            "Equipo en buen estado general.", tamanoPantalla: "22 pulgadas"),
        Donacion(131, TipoEquipo.Notebook, "Toshiba", "Satellite C55", EstadoInicialDeclarado.ParaRepuestos, new DateTime(2026, 8, 5), EstadoEquipo.Baja,
            "No enciende."),
        Donacion(134, TipoEquipo.Celular, "Samsung", "Galaxy A10", EstadoInicialDeclarado.Funcional, new DateTime(2026, 9, 18), EstadoEquipo.Cancelado,
            "Donación cancelada antes de la entrega.")
    ];

    public DonacionesController(IDonacionService donacionService)
    {
        _donacionService = donacionService;
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public IActionResult Index(MisDonacionesViewModel filtros)
    {
        // TODO: reemplazar por la consulta real con filtros y paginación
        filtros.Filas = DatosDeEjemplo;
        filtros.Total = DatosDeEjemplo.Count;

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public IActionResult Detalle(int id)
    {
        // TODO: reemplazar por el servicio de consulta real!!! el usuario solo puede ver sus propias donaciones
        var donacion = DatosDeEjemplo.FirstOrDefault(d => d.NroEquipo == id);

        return donacion is null ? NotFound() : View(donacion);
    }

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

    private static DonacionDetalleViewModel Donacion(
        int nroEquipo, TipoEquipo tipo, string marca, string modelo, EstadoInicialDeclarado condicion,
        DateTime fecha, EstadoEquipo estado, string? observaciones = null, string? procesador = null,
        string? memoriaRam = null, string? almacenamiento = null, string? tamanoPantalla = null,
        string? estadoBateria = null) => new()
        {
            NroEquipo = nroEquipo,
            TipoEquipo = tipo,
            Marca = marca,
            Modelo = modelo,
            EstadoInicialDeclarado = condicion,
            FechaRegistro = fecha,
            EstadoEquipo = estado,
            Donante = "Valeria Acosta",
            Observaciones = observaciones,
            Procesador = procesador,
            MemoriaRam = memoriaRam,
            Almacenamiento = almacenamiento,
            TamanoPantalla = tamanoPantalla,
            EstadoBateria = estadoBateria,
            Historial = ArmarHistorial(estado, fecha, condicion)
        };

    private static IReadOnlyList<HitoViewModel> ArmarHistorial(EstadoEquipo estado, DateTime fecha, EstadoInicialDeclarado condicion)
    {
        if (estado == EstadoEquipo.Cancelado)
        {
            return
            [
                Hito("Donación registrada", fecha, "gift", EstadoHito.Completado),
                Hito("Donación cancelada", fecha.AddDays(1), "x", EstadoHito.Cancelado, "Cancelada por el donante antes de la recepción.")
            ];
        }

        var etapaActual = estado switch
        {
            EstadoEquipo.PendienteRecepcion => 1,
            EstadoEquipo.Asignado => 4,
            EstadoEquipo.Entregado => 5,
            _ => 3
        };

        var dictamen = estado switch
        {
            EstadoEquipo.EnReparacion => "Necesita reparación",
            EstadoEquipo.Baja => "No recuperable",
            _ => "Funcional"
        };

        var condicionTexto = condicion switch
        {
            EstadoInicialDeclarado.ConFallas => "con fallas",
            EstadoInicialDeclarado.ParaRepuestos => "para repuestos",
            _ => "funcional"
        };

        var pasos = new List<HitoViewModel>
        {
            Hito("Donación registrada", fecha, "gift", EstadoHito.Completado, $"Estado inicial declarado: {condicionTexto}."),
            Hito("Equipo recibido", fecha.AddDays(1), "package", EstadoHito.Completado),
            Hito("Diagnóstico técnico", fecha.AddDays(2), "wrench", EstadoHito.Completado, $"Dictamen: {dictamen}."),
            Hito("Asignación a una solicitud", fecha.AddDays(8), "swap", EstadoHito.Completado),
            Hito("Entrega completada", fecha.AddDays(12), "truck", EstadoHito.Completado)
        };

        if (estado == EstadoEquipo.Baja)
        {
            pasos[3] = Hito("Baja del equipo", fecha.AddDays(3), "ban", EstadoHito.Completado, "No recuperable: se destina a repuestos.");
            pasos.RemoveAt(4);
        }

        return pasos
            .Select((paso, i) => i < etapaActual
                ? paso
                : Hito(paso.Titulo, null, paso.Icono, i == etapaActual ? EstadoHito.Actual : EstadoHito.Pendiente))
            .ToList();
    }

    private static HitoViewModel Hito(string titulo, DateTime? fecha, string icono, EstadoHito estado, string? texto = null) => new()
    {
        Titulo = titulo,
        Fecha = fecha,
        Icono = icono,
        Estado = estado,
        Texto = texto
    };
}