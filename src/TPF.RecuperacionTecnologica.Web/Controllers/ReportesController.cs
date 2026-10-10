using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

// TODO: restringir a la política SoloAdministrador
[Authorize]
public class ReportesController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new GenerarReporteViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Generar(GenerarReporteViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), modelo);
        }

        var rango = new { desde = modelo.Desde, hasta = modelo.Hasta };

        return modelo.Tipo switch
        {
            TipoReporte.EntregasPorPeriodo => RedirectToAction(nameof(EntregasPorPeriodo), rango),
            TipoReporte.TiposSolicitados => RedirectToAction(nameof(TiposSolicitados), rango),
            _ => RedirectToAction(nameof(EquiposPorEstado), rango)
        };
    }

    [HttpGet]
    public IActionResult EquiposPorEstado(DateTime? desde, DateTime? hasta)
    {
        // TODO: reemplazar estos datos de ejemplo por el cálculo real según el rango de fechas
        return View(new ReporteEquiposPorEstadoViewModel
        {
            Desde = desde,
            Hasta = hasta,
            Estados =
            [
                new() { Estado = EstadoEquipo.Disponible, Cantidad = 42 },
                new() { Estado = EstadoEquipo.PendienteRecepcion, Cantidad = 14 },
                new() { Estado = EstadoEquipo.EnReparacion, Cantidad = 18 },
                new() { Estado = EstadoEquipo.Asignado, Cantidad = 12 },
                new() { Estado = EstadoEquipo.Entregado, Cantidad = 24 },
                new() { Estado = EstadoEquipo.Baja, Cantidad = 8 }
            ]
        });
    }

    [HttpGet]
    public IActionResult EntregasPorPeriodo(DateTime? desde, DateTime? hasta, AgrupacionPeriodo agrupacion = AgrupacionPeriodo.Mes)
    {
        // TODO: reemplazar estos datos de ejemplo por el cálculo real según el rango y la agrupación
        IReadOnlyList<PeriodoEntregasViewModel> filas = agrupacion switch
        {
            AgrupacionPeriodo.Trimestre =>
            [
                new() { Etiqueta = "2.º trimestre 2026", Cantidad = 36 },
                new() { Etiqueta = "3.er trimestre 2026", Cantidad = 63 }
            ],
            AgrupacionPeriodo.Anio =>
            [
                new() { Etiqueta = "2026", Cantidad = 99 }
            ],
            _ =>
            [
                new() { Etiqueta = "Abril 2026", Cantidad = 9 },
                new() { Etiqueta = "Mayo 2026", Cantidad = 12 },
                new() { Etiqueta = "Junio 2026", Cantidad = 15 },
                new() { Etiqueta = "Julio 2026", Cantidad = 18 },
                new() { Etiqueta = "Agosto 2026", Cantidad = 21 },
                new() { Etiqueta = "Septiembre 2026", Cantidad = 24 }
            ]
        };

        return View(new ReporteEntregasPorPeriodoViewModel
        {
            Desde = desde,
            Hasta = hasta,
            Agrupacion = agrupacion,
            Filas = filas
        });
    }

    [HttpGet]
    public IActionResult TiposSolicitados(DateTime? desde, DateTime? hasta)
    {
        // TODO: reemplazar estos datos de ejemplo por el cálculo real según el rango de fechas
        return View(new ReporteTiposSolicitadosViewModel
        {
            Desde = desde,
            Hasta = hasta,
            Filas =
            [
                new() { TipoEquipo = TipoEquipo.Notebook, Solicitudes = 38 },
                new() { TipoEquipo = TipoEquipo.PcEscritorio, Solicitudes = 27 },
                new() { TipoEquipo = TipoEquipo.Monitor, Solicitudes = 18 },
                new() { TipoEquipo = TipoEquipo.Celular, Solicitudes = 9 },
                new() { TipoEquipo = TipoEquipo.Periferico, Solicitudes = 6 }
            ]
        });
    }
}
