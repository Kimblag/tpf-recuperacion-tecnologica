using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class SolicitudesController : Controller
{
    private readonly IDonacionService _donacionService;

    public SolicitudesController(IDonacionService donacionService)
    {
        _donacionService = donacionService;
    }

    private sealed record SolicitudEjemplo(
        int NroSolicitud, int NroEquipo, TipoEquipo TipoEquipo, string MarcaYModelo,
        TipoSolicitante TipoSolicitante, OrigenSolicitud OrigenSolicitud, DateTime Fecha,
        EstadoSolicitud Estado, string Motivo, DateTime? FechaAsignacion = null,
        DateTime? LimiteRetiro = null, DateTime? FechaEntrega = null);

    // TODO: reemplazar estos datos de ejemplo por las solicitudes reales del Usuario (#149)
    private static readonly IReadOnlyList<SolicitudEjemplo> Ejemplos =
    [
        new(31, 126, TipoEquipo.Monitor, "Samsung S24F350", TipoSolicitante.Particular, OrigenSolicitud.Autoservicio,
            new DateTime(2026, 9, 8), EstadoSolicitud.Pendiente, "Necesito un monitor para el estudio de mi hija."),
        new(25, 125, TipoEquipo.Notebook, "HP ProBook 450 G5", TipoSolicitante.Particular, OrigenSolicitud.Autoservicio,
            new DateTime(2026, 8, 28), EstadoSolicitud.Asignada, "Trabajo remoto y clases de mis hijos.",
            new DateTime(2026, 9, 15), new DateTime(2026, 9, 25)),
        new(20, 136, TipoEquipo.Monitor, "Dell E2216H", TipoSolicitante.Particular, OrigenSolicitud.Autoservicio,
            new DateTime(2026, 8, 20), EstadoSolicitud.Cancelada, "Ya no lo necesito."),
        new(19, 129, TipoEquipo.PcEscritorio, "Lenovo ThinkCentre M710", TipoSolicitante.Particular, OrigenSolicitud.Asistida,
            new DateTime(2026, 8, 12), EstadoSolicitud.Completada, "Computadora para el hogar.",
            new DateTime(2026, 8, 14), new DateTime(2026, 8, 24), new DateTime(2026, 8, 21)),
        new(17, 128, TipoEquipo.Periferico, "Logitech K120", TipoSolicitante.Particular, OrigenSolicitud.Autoservicio,
            new DateTime(2026, 8, 5), EstadoSolicitud.Cerrada, "Teclado para la PC de casa.",
            new DateTime(2026, 8, 8), new DateTime(2026, 8, 18))
    ];

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public IActionResult Index(MisSolicitudesViewModel filtros)
    {
        // TODO: reemplazar por la consulta real con filtros y paginación (#149)
        var texto = filtros.Busqueda?.Trim();

        var filtradas = Ejemplos
            .Where(s => (filtros.Estado is null || s.Estado == filtros.Estado)
                && (string.IsNullOrEmpty(texto)
                    || $"SOL-{s.NroSolicitud:D6}".Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || s.NroEquipo.ToString("D6").Contains(texto)
                    || s.MarcaYModelo.Contains(texto, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(s => s.Fecha)
            .ThenByDescending(s => s.NroSolicitud)
            .ToList();

        filtros.Total = filtradas.Count;
        filtros.Pagina = Math.Clamp(filtros.Pagina, 1, filtros.TotalPaginas);
        filtros.Filas = filtradas
            .Skip((filtros.Pagina - 1) * MisSolicitudesViewModel.TamanoPagina)
            .Take(MisSolicitudesViewModel.TamanoPagina)
            .Select(s => new SolicitudFilaViewModel
            {
                NroSolicitud = s.NroSolicitud,
                NroEquipo = s.NroEquipo,
                TipoEquipo = s.TipoEquipo,
                MarcaYModelo = s.MarcaYModelo,
                TipoSolicitante = s.TipoSolicitante,
                OrigenSolicitud = s.OrigenSolicitud,
                FechaSolicitud = s.Fecha,
                EstadoSolicitud = s.Estado
            })
            .ToList();

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public IActionResult Detalle(int id)
    {
        // TODO: reemplazar por la consulta real, limitada al Usuario propietario (#149)
        var solicitud = Ejemplos.FirstOrDefault(s => s.NroSolicitud == id);

        if (solicitud is null)
        {
            return NotFound();
        }

        return View(new SolicitudDetalleViewModel
        {
            NroSolicitud = solicitud.NroSolicitud,
            NroEquipo = solicitud.NroEquipo,
            TipoEquipo = solicitud.TipoEquipo,
            MarcaYModelo = solicitud.MarcaYModelo,
            TipoSolicitante = solicitud.TipoSolicitante,
            MotivoSolicitud = solicitud.Motivo,
            OrigenSolicitud = solicitud.OrigenSolicitud,
            FechaSolicitud = solicitud.Fecha,
            EstadoSolicitud = solicitud.Estado,
            Hitos = ArmarHitos(solicitud)
        });
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Registrar(int nroEquipo)
    {
        var equipo = await _donacionService.ObtenerDetalleDisponibleAsync(nroEquipo);

        return View(new FormularioSolicitudViewModel
        {
            NroEquipo = nroEquipo,
            EquipoDisponible = equipo is not null,
            TipoEquipo = equipo?.TipoEquipo ?? TipoEquipo.Notebook,
            MarcaYModelo = equipo?.MarcaYModelo ?? string.Empty,
            Solicitante = User.FindFirst(ClaimsSesion.Nombre)?.Value ?? string.Empty,
            OrigenSolicitud = OrigenSolicitud.Autoservicio,
            FechaSolicitud = DateTime.Today,
            EstadoSolicitud = EstadoSolicitud.Pendiente
        });
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public IActionResult Editar(int id)
    {
        // TODO: reemplazar por la consulta real de la solicitud propia (#119)
        var solicitud = Ejemplos.FirstOrDefault(s => s.NroSolicitud == id);

        if (solicitud is null)
        {
            return NotFound();
        }

        return View(new FormularioSolicitudViewModel
        {
            NroEquipo = solicitud.NroEquipo,
            NroSolicitud = solicitud.NroSolicitud,
            TipoSolicitante = solicitud.TipoSolicitante,
            MotivoSolicitud = solicitud.Motivo,
            Solicitante = User.FindFirst(ClaimsSesion.Nombre)?.Value ?? string.Empty,
            TipoEquipo = solicitud.TipoEquipo,
            MarcaYModelo = solicitud.MarcaYModelo,
            OrigenSolicitud = solicitud.OrigenSolicitud,
            FechaSolicitud = solicitud.Fecha,
            EstadoSolicitud = solicitud.Estado
        });
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public IActionResult Historial()
    {
        // TODO: reemplazar por los movimientos reales de todas las solicitudes propias (#149)
        var movimientos = Ejemplos
            .SelectMany(s => ArmarHitos(s)
                .Where(h => h.Estado != EstadoHito.Pendiente && h.Estado != EstadoHito.Actual && h.Fecha is not null)
                .Select(h => (Solicitud: s, Hito: h)))
            .OrderByDescending(x => x.Hito.Fecha)
            .Select(x => new HitoViewModel
            {
                Titulo = $"{x.Hito.Titulo} · SOL-{x.Solicitud.NroSolicitud:D6}",
                Fecha = x.Hito.Fecha,
                Icono = x.Hito.Icono,
                Estado = x.Hito.Estado,
                Texto = x.Solicitud.MarcaYModelo
            })
            .ToList();

        return View(new HistorialSolicitudesViewModel { Movimientos = movimientos });
    }

    private static IReadOnlyList<HitoViewModel> ArmarHitos(SolicitudEjemplo solicitud)
    {
        var hitos = new List<HitoViewModel>
        {
            new()
            {
                Titulo = "Solicitud registrada",
                Fecha = solicitud.Fecha,
                Icono = "tray",
                Estado = EstadoHito.Completado,
                Texto = solicitud.OrigenSolicitud == OrigenSolicitud.Asistida ? "Registrada con ayuda de una persona coordinadora." : "Registrada por la propia persona."
            }
        };

        switch (solicitud.Estado)
        {
            case EstadoSolicitud.Cancelada:
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Solicitud cancelada",
                    Fecha = solicitud.Fecha.AddDays(2),
                    Icono = "x",
                    Estado = EstadoHito.Cancelado,
                    Texto = "Cancelada por la persona solicitante."
                });
                break;

            case EstadoSolicitud.Cerrada:
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Equipo asignado",
                    Fecha = solicitud.FechaAsignacion,
                    Icono = "swap",
                    Estado = EstadoHito.Completado
                });
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Solicitud cerrada",
                    Fecha = solicitud.LimiteRetiro?.AddDays(1),
                    Icono = "clock",
                    Estado = EstadoHito.Cancelado,
                    Texto = "Venció el plazo de retiro. La asignación fue liberada."
                });
                break;

            case EstadoSolicitud.Pendiente:
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Asignación de equipo",
                    Icono = "swap",
                    Estado = EstadoHito.Actual,
                    Texto = "En espera de una persona coordinadora."
                });
                hitos.Add(new HitoViewModel { Titulo = "Entrega del equipo", Icono = "truck", Estado = EstadoHito.Pendiente });
                break;

            case EstadoSolicitud.Asignada:
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Equipo asignado",
                    Fecha = solicitud.FechaAsignacion,
                    Icono = "swap",
                    Estado = EstadoHito.Completado,
                    Texto = $"Hay que retirarlo hasta el {solicitud.LimiteRetiro:dd/MM/yyyy}."
                });
                hitos.Add(new HitoViewModel { Titulo = "Entrega del equipo", Icono = "truck", Estado = EstadoHito.Pendiente });
                break;

            default:
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Equipo asignado",
                    Fecha = solicitud.FechaAsignacion,
                    Icono = "swap",
                    Estado = EstadoHito.Completado
                });
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Entrega completada",
                    Fecha = solicitud.FechaEntrega,
                    Icono = "truck",
                    Estado = EstadoHito.Completado
                });
                break;
        }

        return hitos;
    }
}
