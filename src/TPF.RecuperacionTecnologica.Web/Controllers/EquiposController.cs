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
}
