using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class EquiposController : Controller
{
    // TODO: reemplazar estos datos de ejemplo por la consulta real del servicio de equipos
    private static readonly IReadOnlyList<EquipoFilaViewModel> DatosDeEjemplo =
    [
        Equipo(127, TipoEquipo.Notebook, "Lenovo", "ThinkPad T440", EstadoInicialDeclarado.ConFallas, new DateTime(2026, 9, 17), EstadoEquipo.PendienteRecepcion),
        Equipo(132, TipoEquipo.PcEscritorio, "HP", "ProDesk 400", EstadoInicialDeclarado.Funcional, new DateTime(2026, 9, 16), EstadoEquipo.EnReparacion),
        Equipo(130, TipoEquipo.Notebook, "Dell", "Latitude 3410", EstadoInicialDeclarado.ConFallas, new DateTime(2026, 9, 15), EstadoEquipo.PendienteRecepcion),
        Equipo(133, TipoEquipo.Monitor, "LG", "22MK430", EstadoInicialDeclarado.Funcional, new DateTime(2026, 9, 18), EstadoEquipo.PendienteRecepcion),
        Equipo(125, TipoEquipo.Notebook, "Acer", "Aspire 5", EstadoInicialDeclarado.Funcional, new DateTime(2026, 8, 25), EstadoEquipo.Asignado),
        Equipo(123, TipoEquipo.Notebook, "Dell", "Latitude E6440", EstadoInicialDeclarado.Funcional, new DateTime(2026, 8, 24), EstadoEquipo.Disponible),
        Equipo(129, TipoEquipo.Monitor, "Samsung", "S22F350", EstadoInicialDeclarado.Funcional, new DateTime(2026, 8, 8), EstadoEquipo.Entregado),
        Equipo(131, TipoEquipo.Notebook, "Toshiba", "Satellite C55", EstadoInicialDeclarado.ParaRepuestos, new DateTime(2026, 8, 5), EstadoEquipo.Baja),
        Equipo(134, TipoEquipo.Celular, "Samsung", "Galaxy A10", EstadoInicialDeclarado.Funcional, new DateTime(2026, 9, 18), EstadoEquipo.Cancelado),
        Equipo(136, TipoEquipo.Periferico, "Logitech", "K120", EstadoInicialDeclarado.Funcional, new DateTime(2026, 9, 19), EstadoEquipo.PendienteRecepcion)
    ];

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public IActionResult Index(EquipoListadoViewModel filtros)
    {
        // TODO: reemplazar por la consulta real con filtros, orden y paginación
        filtros.Filas = DatosDeEjemplo
            .Where(e => Coincide(e, filtros))
            .OrderBy(e => e.FechaRegistro)
            .ToList();
        filtros.Total = filtros.Filas.Count;

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public IActionResult Pendientes(EquipoListadoViewModel filtros)
    {
        // TODO: reemplazar por la consulta real de equipos pendientes de diagnóstico
        filtros.Estado = null;
        filtros.Filas = DatosDeEjemplo
            .Where(e => e.EstadoEquipo == EstadoEquipo.PendienteRecepcion && Coincide(e, filtros))
            .OrderBy(e => e.FechaRegistro)
            .ToList();
        filtros.Total = filtros.Filas.Count;

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public IActionResult Detalle(int id)
    {
        // TODO: reemplazar por la consulta real del servicio de equipos
        var equipo = DatosDeEjemplo.FirstOrDefault(e => e.NroEquipo == id);

        return equipo is null ? NotFound() : View(DetalleDeEjemplo(equipo));
    }

    [HttpGet]
    [Authorize(Policy = "PersonalInterno")]
    public IActionResult Historial(int id)
    {
        // TODO: reemplazar por la consulta real del historial de diagnósticos
        var equipo = DatosDeEjemplo.FirstOrDefault(e => e.NroEquipo == id);

        if (equipo is null)
        {
            return NotFound();
        }

        var diagnosticos = DiagnosticosDeEjemplo(equipo);

        return View(new HistorialDiagnosticosViewModel
        {
            NroEquipo = equipo.NroEquipo,
            TipoEquipo = equipo.TipoEquipo,
            Marca = equipo.Marca,
            Modelo = equipo.Modelo,
            EstadoEquipo = equipo.EstadoEquipo,
            Vigente = VigenteDeEjemplo(diagnosticos),
            Diagnosticos = diagnosticos
        });
    }

    private static bool Coincide(EquipoFilaViewModel equipo, EquipoListadoViewModel filtros)
    {
        var texto = filtros.Busqueda?.Trim();

        return (filtros.Tipo is null || equipo.TipoEquipo == filtros.Tipo)
            && (filtros.Estado is null || equipo.EstadoEquipo == filtros.Estado)
            && (string.IsNullOrEmpty(texto)
                || equipo.CodigoEquipo.Contains(texto)
                || equipo.MarcaYModelo.Contains(texto, StringComparison.OrdinalIgnoreCase));
    }

    private static EquipoFilaViewModel Equipo(
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

    private static EquipoDetalleTecnicoViewModel DetalleDeEjemplo(EquipoFilaViewModel equipo) => new()
    {
        NroEquipo = equipo.NroEquipo,
        TipoEquipo = equipo.TipoEquipo,
        Marca = equipo.Marca,
        Modelo = equipo.Modelo,
        EstadoInicialDeclarado = equipo.EstadoInicialDeclarado,
        Observaciones = "Observaciones de ejemplo del donante.",
        EstadoEquipo = equipo.EstadoEquipo,
        FechaRegistro = equipo.FechaRegistro,
        FechaDisponibilidad = equipo.EstadoEquipo is EstadoEquipo.Disponible or EstadoEquipo.Asignado or EstadoEquipo.Entregado
            ? equipo.FechaRegistro.AddDays(3)
            : null,
        FechaCancelacion = equipo.EstadoEquipo == EstadoEquipo.Cancelado ? equipo.FechaRegistro.AddDays(1) : null,
        MotivoCancelacion = equipo.EstadoEquipo == EstadoEquipo.Cancelado ? "Cancelada por el donante" : null,
        Donante = "Valeria Acosta",
        Procesador = "Intel Core i5",
        MemoriaRam = "8 GB",
        Almacenamiento = "256 GB SSD",
        DiagnosticoVigente = VigenteDeEjemplo(DiagnosticosDeEjemplo(equipo))
    };

    private static List<DiagnosticoFilaViewModel> DiagnosticosDeEjemplo(EquipoFilaViewModel equipo)
    {
        var fecha = equipo.FechaRegistro.AddDays(2);
        var nro = equipo.NroEquipo * 10;

        return equipo.EstadoEquipo switch
        {
            EstadoEquipo.EnReparacion =>
            [
                Diagnostico(nro + 1, TipoDiagnostico.Inicial, fecha, DictamenTecnico.NecesitaReparacion, true)
            ],
            EstadoEquipo.Disponible =>
            [
                Diagnostico(nro + 1, TipoDiagnostico.Inicial, fecha, DictamenTecnico.NecesitaReparacion, false, reemplazadoPor: nro + 2),
                Diagnostico(nro + 2, TipoDiagnostico.Segundo, fecha.AddDays(5), DictamenTecnico.Funcional, true)
            ],
            EstadoEquipo.Asignado or EstadoEquipo.Entregado =>
            [
                Diagnostico(nro + 1, TipoDiagnostico.Inicial, fecha, DictamenTecnico.Funcional, true)
            ],
            EstadoEquipo.Baja =>
            [
                Diagnostico(nro + 1, TipoDiagnostico.Inicial, fecha, DictamenTecnico.NecesitaReparacion, false, reemplazadoPor: nro + 2),
                Diagnostico(nro + 2, TipoDiagnostico.Rectificacion, fecha.AddDays(1), DictamenTecnico.NoRecuperable, true, rectifica: nro + 1)
            ],
            _ => []
        };
    }

    private static DiagnosticoVigenteViewModel? VigenteDeEjemplo(List<DiagnosticoFilaViewModel> diagnosticos)
    {
        var vigente = diagnosticos.FirstOrDefault(d => d.EsVigente);

        return vigente is null
            ? null
            : new DiagnosticoVigenteViewModel
            {
                NroDiagnostico = vigente.NroDiagnostico,
                TipoDiagnostico = vigente.TipoDiagnostico,
                FechaDiagnostico = vigente.FechaDiagnostico,
                Tecnico = "Tomás Ibáñez",
                DictamenTecnico = vigente.DictamenTecnico,
                DetalleFallas = "Falla de ejemplo detectada en la evaluación.",
                DetalleReparacionesRealizadas = vigente.TipoDiagnostico == TipoDiagnostico.Inicial ? null : "Reparación de ejemplo realizada.",
                Observaciones = "Observación de ejemplo.",
                Procesador = "Intel Core i5",
                MemoriaRam = "8 GB",
                Almacenamiento = "256 GB SSD"
            };
    }

    private static DiagnosticoFilaViewModel Diagnostico(
        int nroDiagnostico, TipoDiagnostico tipo, DateTime fecha, DictamenTecnico dictamen, bool vigente,
        int? rectifica = null, int? reemplazadoPor = null) => new()
        {
            NroDiagnostico = nroDiagnostico,
            TipoDiagnostico = tipo,
            FechaDiagnostico = fecha,
            CodigoTecnico = 12,
            DictamenTecnico = dictamen,
            EsVigente = vigente,
            NroDiagnosticoRectificado = rectifica,
            NroDiagnosticoReemplazante = reemplazadoPor
        };
}
