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

    public DiagnosticosController(
        IEquipoConsultaService equipoConsultaService,
        IDiagnosticoService diagnosticoService,
        IPersonalService personalService)
    {
        _equipoConsultaService = equipoConsultaService;
        _diagnosticoService = diagnosticoService;
        _personalService = personalService;
    }

    private sealed record EquipoEjemplo(
        TipoEquipo Tipo, string Marca, string Modelo, EstadoEquipo Estado, EstadoInicialDeclarado Condicion);

    // TODO: reemplazar estos datos de ejemplo por la consulta real del servicio de equipos
    private static readonly IReadOnlyDictionary<int, EquipoEjemplo> EquiposDeEjemplo = new Dictionary<int, EquipoEjemplo>
    {
        [127] = new(TipoEquipo.Notebook, "Lenovo", "ThinkPad T440", EstadoEquipo.PendienteRecepcion, EstadoInicialDeclarado.ConFallas),
        [132] = new(TipoEquipo.PcEscritorio, "HP", "ProDesk 400", EstadoEquipo.EnReparacion, EstadoInicialDeclarado.Funcional),
        [130] = new(TipoEquipo.Notebook, "Dell", "Latitude 3410", EstadoEquipo.PendienteRecepcion, EstadoInicialDeclarado.ConFallas),
        [133] = new(TipoEquipo.Monitor, "LG", "22MK430", EstadoEquipo.PendienteRecepcion, EstadoInicialDeclarado.Funcional),
        [125] = new(TipoEquipo.Notebook, "Acer", "Aspire 5", EstadoEquipo.Asignado, EstadoInicialDeclarado.Funcional),
        [123] = new(TipoEquipo.Notebook, "Dell", "Latitude E6440", EstadoEquipo.Disponible, EstadoInicialDeclarado.Funcional),
        [129] = new(TipoEquipo.Monitor, "Samsung", "S22F350", EstadoEquipo.Entregado, EstadoInicialDeclarado.Funcional),
        [131] = new(TipoEquipo.Notebook, "Toshiba", "Satellite C55", EstadoEquipo.Baja, EstadoInicialDeclarado.ParaRepuestos),
        [134] = new(TipoEquipo.Celular, "Samsung", "Galaxy A10", EstadoEquipo.Cancelado, EstadoInicialDeclarado.Funcional),
        [136] = new(TipoEquipo.Periferico, "Logitech", "K120", EstadoEquipo.PendienteRecepcion, EstadoInicialDeclarado.Funcional)
    };

    // TODO: reemplazar estos datos de ejemplo por la consulta real del servicio de diagnósticos
    private static readonly IReadOnlyList<DiagnosticoFilaViewModel> DatosDeEjemplo =
    [
        Diagnostico(1321, 132, TipoDiagnostico.Inicial, new DateTime(2026, 9, 18), DictamenTecnico.NecesitaReparacion, true),
        Diagnostico(1232, 123, TipoDiagnostico.Segundo, new DateTime(2026, 8, 31), DictamenTecnico.Funcional, true),
        Diagnostico(1231, 123, TipoDiagnostico.Inicial, new DateTime(2026, 8, 26), DictamenTecnico.NecesitaReparacion, false, reemplazadoPor: 1232),
        Diagnostico(1251, 125, TipoDiagnostico.Inicial, new DateTime(2026, 8, 27), DictamenTecnico.Funcional, true),
        Diagnostico(1291, 129, TipoDiagnostico.Inicial, new DateTime(2026, 8, 10), DictamenTecnico.Funcional, true),
        Diagnostico(1312, 131, TipoDiagnostico.Rectificacion, new DateTime(2026, 8, 8), DictamenTecnico.NoRecuperable, true, rectifica: 1311),
        Diagnostico(1311, 131, TipoDiagnostico.Inicial, new DateTime(2026, 8, 7), DictamenTecnico.NecesitaReparacion, false, reemplazadoPor: 1312)
    ];

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public IActionResult Index(DiagnosticoListadoViewModel filtros)
    {
        // TODO: reemplazar por la consulta real con filtros, orden y paginación
        var texto = filtros.Busqueda?.Trim();

        filtros.Filas = DatosDeEjemplo
            .Where(d => (filtros.Tipo is null || d.TipoDiagnostico == filtros.Tipo)
                && (filtros.Dictamen is null || d.DictamenTecnico == filtros.Dictamen)
                && (string.IsNullOrEmpty(texto)
                    || d.CodigoDiagnostico.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || d.CodigoEquipo.Contains(texto)))
            .OrderByDescending(d => d.FechaDiagnostico)
            .ThenByDescending(d => d.NroDiagnostico)
            .ToList();
        filtros.Total = filtros.Filas.Count;

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public IActionResult Detalle(int id)
    {
        // TODO: reemplazar por la consulta real del servicio de diagnósticos
        var diagnostico = DatosDeEjemplo.FirstOrDefault(d => d.NroDiagnostico == id);

        if (diagnostico is null || !EquiposDeEjemplo.TryGetValue(diagnostico.NroEquipo, out var equipo))
        {
            return NotFound();
        }

        return View(new DiagnosticoDetalleViewModel
        {
            NroDiagnostico = diagnostico.NroDiagnostico,
            TipoDiagnostico = diagnostico.TipoDiagnostico,
            FechaDiagnostico = diagnostico.FechaDiagnostico,
            NroEquipo = diagnostico.NroEquipo,
            TipoEquipo = equipo.Tipo,
            Marca = equipo.Marca,
            Modelo = equipo.Modelo,
            EstadoEquipo = equipo.Estado,
            CodigoTecnico = diagnostico.CodigoTecnico,
            Tecnico = "Tomás Ibáñez",
            DictamenTecnico = diagnostico.DictamenTecnico,
            EsVigente = diagnostico.EsVigente,
            NroDiagnosticoRectificado = diagnostico.NroDiagnosticoRectificado,
            NroDiagnosticoReemplazante = diagnostico.NroDiagnosticoReemplazante,
            DetalleFallas = "Falla de ejemplo detectada en la evaluación.",
            DetalleReparacionesRealizadas = diagnostico.TipoDiagnostico == TipoDiagnostico.Inicial ? null : "Reparación de ejemplo realizada.",
            Observaciones = "Observación de ejemplo.",
            Procesador = "Intel Core i5",
            MemoriaRam = "8 GB",
            Almacenamiento = "256 GB SSD"
        });
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

        if (tipo == TipoDiagnostico.Rectificacion && formulario.DiagnosticoVigente is not null)
        {
            var vigente = formulario.DiagnosticoVigente;

            formulario.DictamenTecnico = vigente.DictamenTecnico;
            formulario.DetalleFallas = vigente.DetalleFallas;
            formulario.DetalleReparacionesRealizadas = vigente.DetalleReparacionesRealizadas;
            formulario.Observaciones = vigente.Observaciones;
            formulario.Procesador = vigente.Procesador;
            formulario.MemoriaRam = vigente.MemoriaRam;
            formulario.Almacenamiento = vigente.Almacenamiento;
            formulario.TamanoPantalla = vigente.TamanoPantalla;
            formulario.EstadoBateria = vigente.EstadoBateria;
            formulario.Otros = vigente.Otros;
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

        if (tipo == TipoDiagnostico.Inicial)
        {
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var codigoTecnico = identityUserId is null
                ? null
                : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);

            if (codigoTecnico is null)
            {
                return Forbid();
            }

            formulario.NroEquipo = nroEquipo;
            var resultado = await _diagnosticoService.RegistrarInicialAsync(codigoTecnico.Value, formulario);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                CompletarContexto(formulario, ArmarFormulario(nroEquipo, equipo, tipo));

                return View("Formulario", formulario);
            }
        }

        // TODO: reemplazar por el registro real del segundo diagnóstico y la rectificación
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
        DiagnosticoVigente = equipo.DiagnosticoVigente
    };

    private static DiagnosticoFilaViewModel Diagnostico(
        int nroDiagnostico, int nroEquipo, TipoDiagnostico tipo, DateTime fecha, DictamenTecnico dictamen, bool vigente,
        int? rectifica = null, int? reemplazadoPor = null) => new()
        {
            NroDiagnostico = nroDiagnostico,
            NroEquipo = nroEquipo,
            TipoDiagnostico = tipo,
            FechaDiagnostico = fecha,
            CodigoTecnico = 12,
            DictamenTecnico = dictamen,
            EsVigente = vigente,
            NroDiagnosticoRectificado = rectifica,
            NroDiagnosticoReemplazante = reemplazadoPor
        };
}
