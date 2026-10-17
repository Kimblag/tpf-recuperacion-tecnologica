using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Diagnosticos;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class DiagnosticosController : Controller
{
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
    public IActionResult Registrar(int nroEquipo) => MostrarFormulario(nroEquipo, TipoDiagnostico.Inicial);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "PersonalInterno")]
    public IActionResult Registrar(int nroEquipo, DiagnosticoFormularioViewModel modelo) =>
        Guardar(nroEquipo, TipoDiagnostico.Inicial, modelo, "El diagnóstico se registró correctamente.");

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public IActionResult Segundo(int nroEquipo) => MostrarFormulario(nroEquipo, TipoDiagnostico.Segundo);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "PersonalInterno")]
    public IActionResult Segundo(int nroEquipo, DiagnosticoFormularioViewModel modelo) =>
        Guardar(nroEquipo, TipoDiagnostico.Segundo, modelo, "El segundo diagnóstico se registró correctamente.");

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public IActionResult Rectificar(int nroEquipo) => MostrarFormulario(nroEquipo, TipoDiagnostico.Rectificacion);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "PersonalInterno")]
    public IActionResult Rectificar(int nroEquipo, DiagnosticoFormularioViewModel modelo) =>
        Guardar(nroEquipo, TipoDiagnostico.Rectificacion, modelo, "La rectificación se guardó correctamente.");

    private IActionResult MostrarFormulario(int nroEquipo, TipoDiagnostico tipo)
    {
        if (!EquiposDeEjemplo.TryGetValue(nroEquipo, out var equipo))
        {
            return NotFound();
        }

        var redireccion = RedirigirSiCorresponde(nroEquipo, equipo.Estado, tipo, out var bloqueo);

        if (redireccion is not null || bloqueo is not null)
        {
            return redireccion ?? View("NoDisponible", new NoDisponibleViewModel { NroEquipo = nroEquipo, Mensaje = bloqueo! });
        }

        var modelo = ArmarFormulario(nroEquipo, equipo, tipo);

        if (tipo == TipoDiagnostico.Rectificacion && modelo.DiagnosticoVigente is not null)
        {
            // TODO: precargar los datos reales del diagnóstico vigente
            modelo.DictamenTecnico = modelo.DiagnosticoVigente.DictamenTecnico;
            modelo.DetalleFallas = "Falla de ejemplo detectada en la evaluación.";
            modelo.Observaciones = "Observación de ejemplo.";
            modelo.Procesador = "Intel Core i5";
            modelo.MemoriaRam = "8 GB";
            modelo.Almacenamiento = "256 GB SSD";
        }

        return View("Formulario", modelo);
    }

    private IActionResult Guardar(int nroEquipo, TipoDiagnostico tipo, DiagnosticoFormularioViewModel modelo, string mensaje)
    {
        if (!EquiposDeEjemplo.TryGetValue(nroEquipo, out var equipo))
        {
            return NotFound();
        }

        var redireccion = RedirigirSiCorresponde(nroEquipo, equipo.Estado, tipo, out var bloqueo);

        if (redireccion is not null || bloqueo is not null)
        {
            return redireccion ?? View("NoDisponible", new NoDisponibleViewModel { NroEquipo = nroEquipo, Mensaje = bloqueo! });
        }

        var conFallas = modelo.DictamenTecnico is DictamenTecnico.NecesitaReparacion or DictamenTecnico.NoRecuperable;

        if (conFallas && string.IsNullOrWhiteSpace(modelo.DetalleFallas))
        {
            ModelState.AddModelError(nameof(modelo.DetalleFallas), "Las fallas detectadas son obligatorias para este dictamen.");
        }

        if (tipo == TipoDiagnostico.Segundo && string.IsNullOrWhiteSpace(modelo.DetalleReparacionesRealizadas))
        {
            ModelState.AddModelError(nameof(modelo.DetalleReparacionesRealizadas), "El detalle de reparación es obligatorio en el segundo diagnóstico.");
        }

        if (!ModelState.IsValid)
        {
            var contexto = ArmarFormulario(nroEquipo, equipo, tipo);
            modelo.NroEquipo = contexto.NroEquipo;
            modelo.TipoDiagnostico = contexto.TipoDiagnostico;
            modelo.TipoEquipo = contexto.TipoEquipo;
            modelo.Marca = contexto.Marca;
            modelo.Modelo = contexto.Modelo;
            modelo.EstadoInicialDeclarado = contexto.EstadoInicialDeclarado;
            modelo.EstadoEquipo = contexto.EstadoEquipo;
            modelo.ObservacionesDonante = contexto.ObservacionesDonante;
            modelo.ProcesadorDeclarado = contexto.ProcesadorDeclarado;
            modelo.MemoriaRamDeclarada = contexto.MemoriaRamDeclarada;
            modelo.AlmacenamientoDeclarado = contexto.AlmacenamientoDeclarado;
            modelo.DiagnosticoVigente = contexto.DiagnosticoVigente;

            return View("Formulario", modelo);
        }

        // TODO: reemplazar por el registro real del diagnóstico y la transición de estado del equipo
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

    private static DiagnosticoFormularioViewModel ArmarFormulario(int nroEquipo, EquipoEjemplo equipo, TipoDiagnostico tipo) => new()
    {
        NroEquipo = nroEquipo,
        TipoDiagnostico = tipo,
        TipoEquipo = equipo.Tipo,
        Marca = equipo.Marca,
        Modelo = equipo.Modelo,
        EstadoInicialDeclarado = equipo.Condicion,
        EstadoEquipo = equipo.Estado,
        ObservacionesDonante = "Observaciones de ejemplo del donante.",
        ProcesadorDeclarado = "Intel Core i5",
        MemoriaRamDeclarada = "8 GB",
        AlmacenamientoDeclarado = "256 GB SSD",
        DiagnosticoVigente = DatosDeEjemplo.FirstOrDefault(d => d.NroEquipo == nroEquipo && d.EsVigente)
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
