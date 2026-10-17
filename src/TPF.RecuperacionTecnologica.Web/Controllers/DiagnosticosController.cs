using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Diagnosticos;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class DiagnosticosController : Controller
{
    // TODO: reemplazar estos datos de ejemplo por la consulta real del servicio de diagnósticos
    private static readonly IReadOnlyDictionary<int, (TipoEquipo Tipo, string Marca, string Modelo, EstadoEquipo Estado)> EquiposDeEjemplo =
        new Dictionary<int, (TipoEquipo, string, string, EstadoEquipo)>
        {
            [132] = (TipoEquipo.PcEscritorio, "HP", "ProDesk 400", EstadoEquipo.EnReparacion),
            [123] = (TipoEquipo.Notebook, "Dell", "Latitude E6440", EstadoEquipo.Disponible),
            [125] = (TipoEquipo.Notebook, "Acer", "Aspire 5", EstadoEquipo.Asignado),
            [129] = (TipoEquipo.Monitor, "Samsung", "S22F350", EstadoEquipo.Entregado),
            [131] = (TipoEquipo.Notebook, "Toshiba", "Satellite C55", EstadoEquipo.Baja)
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
