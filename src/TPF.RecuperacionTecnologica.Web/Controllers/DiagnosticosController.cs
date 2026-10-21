using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Diagnosticos;
using TPF.RecuperacionTecnologica.Web.Services.Equipos;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.ViewModels.Diagnosticos;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class DiagnosticosController : Controller
{
    private readonly IEquipoConsultaService _equipoConsultaService;

    private readonly IDiagnosticoService _diagnosticoService;
    private readonly IPersonalService _personalService;
    private readonly ILogger<DiagnosticosController> _logger;

    public DiagnosticosController(
        IEquipoConsultaService equipoConsultaService,
        IDiagnosticoService diagnosticoService,
        IPersonalService personalService,
        ILogger<DiagnosticosController> logger)
    {
        _equipoConsultaService = equipoConsultaService;
        _diagnosticoService = diagnosticoService;
        _personalService = personalService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public async Task<IActionResult> Index(DiagnosticoListadoViewModel filtros)
    {
        try
        {
            var resultado = await _diagnosticoService.ListarAsync(
                filtros.Busqueda, filtros.Tipo, filtros.Dictamen, filtros.Pagina, DiagnosticoListadoViewModel.TamanoPagina);

            filtros.Filas = resultado.Elementos;
            filtros.Total = resultado.Total;
            filtros.Pagina = resultado.Pagina;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el listado de diagnósticos");
            filtros.ErrorAlCargar = true;
        }

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public async Task<IActionResult> Detalle(int id)
    {
        var diagnostico = await _diagnosticoService.ObtenerDetalleAsync(id);

        return diagnostico is null ? NotFound() : View(diagnostico);
    }

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public Task<IActionResult> Registrar(int nroEquipo) => MostrarFormularioAsync(nroEquipo, TipoDiagnostico.Inicial);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "PersonalInterno")]
    public Task<IActionResult> Registrar(int nroEquipo, DiagnosticoFormularioViewModel formulario) =>
        GuardarAsync(nroEquipo, TipoDiagnostico.Inicial, formulario, "El diagnóstico se registró correctamente.");

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public Task<IActionResult> Segundo(int nroEquipo) => MostrarFormularioAsync(nroEquipo, TipoDiagnostico.Segundo);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "PersonalInterno")]
    public Task<IActionResult> Segundo(int nroEquipo, DiagnosticoFormularioViewModel formulario) =>
        GuardarAsync(nroEquipo, TipoDiagnostico.Segundo, formulario, "El segundo diagnóstico se registró correctamente.");

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public Task<IActionResult> Rectificar(int nroEquipo) => MostrarFormularioAsync(nroEquipo, TipoDiagnostico.Rectificacion);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "PersonalInterno")]
    public Task<IActionResult> Rectificar(int nroEquipo, DiagnosticoFormularioViewModel formulario) =>
        GuardarAsync(nroEquipo, TipoDiagnostico.Rectificacion, formulario, "La rectificación se guardó correctamente.");

    private async Task<IActionResult> MostrarFormularioAsync(int nroEquipo, TipoDiagnostico tipo)
    {
        var equipo = await _equipoConsultaService.ObtenerDetalleAsync(nroEquipo);

        if (equipo is null)
        {
            return NotFound();
        }

        var redireccion = RedirigirSiCorresponde(nroEquipo, equipo.EstadoEquipo, tipo, out var bloqueo);

        if (redireccion is not null || bloqueo is not null)
        {
            return redireccion ?? View("NoDisponible", new NoDisponibleViewModel { NroEquipo = nroEquipo, Mensaje = bloqueo! });
        }

        var formulario = ArmarFormulario(nroEquipo, equipo, tipo);

        // las características verificadas se precargan para que el Técnico solo corrija lo que cambió
        if (tipo != TipoDiagnostico.Inicial && formulario.DiagnosticoVigente is not null)
        {
            var vigente = formulario.DiagnosticoVigente;

            formulario.Procesador = vigente.Procesador;
            formulario.MemoriaRam = vigente.MemoriaRam;
            formulario.Almacenamiento = vigente.Almacenamiento;
            formulario.TamanoPantalla = vigente.TamanoPantalla;
            formulario.EstadoBateria = vigente.EstadoBateria;
            formulario.Otros = vigente.Otros;

            // las fallas se precargan para que el Técnico las conserve o las corrija según lo reparado
            formulario.DetalleFallas = vigente.DetalleFallas;

            // el segundo diagnóstico es una nueva evaluación; solo la rectificación parte del resto del vigente
            if (tipo == TipoDiagnostico.Rectificacion)
            {
                formulario.DictamenTecnico = vigente.DictamenTecnico;
                formulario.DetalleReparacionesRealizadas = vigente.DetalleReparacionesRealizadas;
                formulario.Observaciones = vigente.Observaciones;
            }
        }

        return View("Formulario", formulario);
    }

    private async Task<IActionResult> GuardarAsync(int nroEquipo, TipoDiagnostico tipo, DiagnosticoFormularioViewModel formulario, string mensaje)
    {
        var equipo = await _equipoConsultaService.ObtenerDetalleAsync(nroEquipo);

        if (equipo is null)
        {
            return NotFound();
        }

        var redireccion = RedirigirSiCorresponde(nroEquipo, equipo.EstadoEquipo, tipo, out var bloqueo);

        if (redireccion is not null || bloqueo is not null)
        {
            return redireccion ?? View("NoDisponible", new NoDisponibleViewModel { NroEquipo = nroEquipo, Mensaje = bloqueo! });
        }

        var conFallas = formulario.DictamenTecnico is DictamenTecnico.NecesitaReparacion or DictamenTecnico.NoRecuperable;

        if (conFallas && string.IsNullOrWhiteSpace(formulario.DetalleFallas))
        {
            ModelState.AddModelError(nameof(formulario.DetalleFallas), "Las fallas detectadas son obligatorias para este dictamen.");
        }

        if (tipo == TipoDiagnostico.Segundo && string.IsNullOrWhiteSpace(formulario.DetalleReparacionesRealizadas))
        {
            ModelState.AddModelError(nameof(formulario.DetalleReparacionesRealizadas), "El detalle de reparación es obligatorio en el segundo diagnóstico.");
        }

        if (!ModelState.IsValid)
        {
            CompletarContexto(formulario, ArmarFormulario(nroEquipo, equipo, tipo));

            return View("Formulario", formulario);
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var codigoTecnico = identityUserId is null
            ? null
            : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);

        if (codigoTecnico is null)
        {
            return Forbid();
        }

        formulario.NroEquipo = nroEquipo;
        var resultado = tipo switch
        {
            TipoDiagnostico.Inicial => await _diagnosticoService.RegistrarInicialAsync(codigoTecnico.Value, formulario),
            TipoDiagnostico.Segundo => await _diagnosticoService.RegistrarSegundoAsync(codigoTecnico.Value, formulario),
            _ => await _diagnosticoService.RegistrarRectificacionAsync(codigoTecnico.Value, formulario)
        };

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            CompletarContexto(formulario, ArmarFormulario(nroEquipo, equipo, tipo));

            return View("Formulario", formulario);
        }

        TempData["Mensaje"] = mensaje;
        return RedirectToAction("Detalle", "Equipos", new { id = nroEquipo });
    }

    private IActionResult? RedirigirSiCorresponde(int nroEquipo, EstadoEquipo estado, TipoDiagnostico tipo, out string? bloqueo)
    {
        bloqueo = null;

        var permitidas = estado switch
        {
            EstadoEquipo.PendienteRecepcion => new[] { "Registrar" },
            EstadoEquipo.EnReparacion => new[] { "Segundo", "Rectificar" },
            EstadoEquipo.Disponible or EstadoEquipo.Baja => new[] { "Rectificar" },
            _ => Array.Empty<string>()
        };

        if (permitidas.Length == 0)
        {
            bloqueo = estado == EstadoEquipo.Cancelado
                ? "La donación fue cancelada. El equipo no se puede diagnosticar."
                : "El equipo ya fue asignado o entregado. Su diagnóstico no se puede modificar.";

            return null;
        }

        var actual = tipo switch
        {
            TipoDiagnostico.Segundo => "Segundo",
            TipoDiagnostico.Rectificacion => "Rectificar",
            _ => "Registrar"
        };

        return permitidas.Contains(actual)
            ? null
            : RedirectToAction(permitidas[0], new { nroEquipo });
    }

    // vuelve a cargar los datos del equipo, que no viajan en el formulario enviado
    private static void CompletarContexto(DiagnosticoFormularioViewModel formulario, DiagnosticoFormularioViewModel contexto)
    {
        formulario.NroEquipo = contexto.NroEquipo;
        formulario.TipoDiagnostico = contexto.TipoDiagnostico;
        formulario.TipoEquipo = contexto.TipoEquipo;
        formulario.Marca = contexto.Marca;
        formulario.Modelo = contexto.Modelo;
        formulario.EstadoInicialDeclarado = contexto.EstadoInicialDeclarado;
        formulario.EstadoEquipo = contexto.EstadoEquipo;
        formulario.ObservacionesDonante = contexto.ObservacionesDonante;
        formulario.ProcesadorDeclarado = contexto.ProcesadorDeclarado;
        formulario.MemoriaRamDeclarada = contexto.MemoriaRamDeclarada;
        formulario.AlmacenamientoDeclarado = contexto.AlmacenamientoDeclarado;
        formulario.TamanoPantallaDeclarado = contexto.TamanoPantallaDeclarado;
        formulario.EstadoBateriaDeclarado = contexto.EstadoBateriaDeclarado;
        formulario.OtrosDeclarado = contexto.OtrosDeclarado;
        formulario.DiagnosticoVigente = contexto.DiagnosticoVigente;
    }

    private static DiagnosticoFormularioViewModel ArmarFormulario(int nroEquipo, EquipoDetalleTecnicoViewModel equipo, TipoDiagnostico tipo) => new()
    {
        NroEquipo = nroEquipo,
        TipoDiagnostico = tipo,
        TipoEquipo = equipo.TipoEquipo,
        Marca = equipo.Marca,
        Modelo = equipo.Modelo,
        EstadoInicialDeclarado = equipo.EstadoInicialDeclarado,
        EstadoEquipo = equipo.EstadoEquipo,
        ObservacionesDonante = equipo.Observaciones,
        ProcesadorDeclarado = equipo.Procesador,
        MemoriaRamDeclarada = equipo.MemoriaRam,
        AlmacenamientoDeclarado = equipo.Almacenamiento,
        TamanoPantallaDeclarado = equipo.TamanoPantalla,
        EstadoBateriaDeclarado = equipo.EstadoBateria,
        OtrosDeclarado = equipo.Otros,
        DiagnosticoVigente = equipo.DiagnosticoVigente
    };
}
