using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Home;
using TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (!User.HasClaim(c => c.Type == ClaimsSesion.TipoUsuario))
        {
            return View();
        }

        // TODO: reemplazar estos datos de ejemplo por los reales del usuario externo
        return View("InicioUsuario", new InicioUsuarioViewModel
        {
            DonacionesActivas = 3,
            DonacionesPendientesDeRecepcion = 1,
            SolicitudesPendientes = 1,
            EquiposDisponibles = 42,
            EntregasRecibidas = 1,
            DonacionesRecientes =
            [
                Donacion(127, TipoEquipo.Notebook, "Lenovo", "ThinkPad T440", new DateTime(2026, 9, 17), EstadoEquipo.PendienteRecepcion),
                Donacion(132, TipoEquipo.PcEscritorio, "HP", "ProDesk 400", new DateTime(2026, 9, 16), EstadoEquipo.EnReparacion),
                Donacion(123, TipoEquipo.Notebook, "Dell", "Latitude E6440", new DateTime(2026, 8, 24), EstadoEquipo.Disponible),
                Donacion(129, TipoEquipo.Monitor, "Samsung", "S22F350", new DateTime(2026, 8, 8), EstadoEquipo.Entregado)
            ],
            SolicitudesRecientes =
            [
                Solicitud(31, 126, new DateTime(2026, 9, 8), EstadoSolicitud.Pendiente),
                Solicitud(25, 125, new DateTime(2026, 8, 28), EstadoSolicitud.Asignada),
                Solicitud(20, 136, new DateTime(2026, 8, 20), EstadoSolicitud.Cancelada),
                Solicitud(19, 129, new DateTime(2026, 8, 12), EstadoSolicitud.Completada)
            ],
            Actividad =
            [
                new() { Icono = "gift", TextoAntes = "Donación del equipo ", Codigo = "000127", TextoDespues = " registrada", Detalle = "17/09/2026 · Pendiente de recepción", EsExito = true },
                new() { Icono = "swap", TextoAntes = "Equipo ", Codigo = "000125", TextoDespues = " asignado a tu solicitud", Detalle = "15/09/2026 · Retirar hasta el 25/09" },
                new() { Icono = "tray", TextoAntes = "Solicitud ", Codigo = "SOL-000031", TextoDespues = " registrada", Detalle = "08/09/2026 · Autoservicio", EsExito = true },
                new() { Icono = "check", TextoAntes = "Entrega ", Codigo = "ENT-000012", TextoDespues = " completada", Detalle = "21/08/2026 · Equipo 000129" }
            ]
        });
    }

    private static DonacionFilaViewModel Donacion(
        int nroEquipo, TipoEquipo tipo, string marca, string modelo, DateTime fecha, EstadoEquipo estado) => new()
        {
            NroEquipo = nroEquipo,
            TipoEquipo = tipo,
            Marca = marca,
            Modelo = modelo,
            EstadoInicialDeclarado = EstadoInicialDeclarado.Funcional,
            FechaRegistro = fecha,
            EstadoEquipo = estado
        };

    private static SolicitudFilaViewModel Solicitud(int nroSolicitud, int nroEquipo, DateTime fecha, EstadoSolicitud estado) => new()
    {
        NroSolicitud = nroSolicitud,
        NroEquipo = nroEquipo,
        TipoSolicitante = TipoSolicitante.Particular,
        OrigenSolicitud = OrigenSolicitud.Autoservicio,
        FechaSolicitud = fecha,
        EstadoSolicitud = estado
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