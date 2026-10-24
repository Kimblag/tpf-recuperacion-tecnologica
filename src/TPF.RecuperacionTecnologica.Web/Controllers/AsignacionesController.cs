using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.ViewModels.Asignaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class AsignacionesController : Controller
{
    private const int DiasLimiteRetiroDeEjemplo = 10;

    private readonly IDonacionService _donacionService;
    private readonly IPersonalService _personalService;

    public AsignacionesController(IDonacionService donacionService, IPersonalService personalService)
    {
        _donacionService = donacionService;
        _personalService = personalService;
    }

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public IActionResult Index(AsignacionListadoViewModel filtros)
    {
        if (!Request.Query.ContainsKey(nameof(AsignacionListadoViewModel.Estado)))
        {
            filtros.Estado = EstadoAsignacion.Vigente;
        }

        // TODO: reemplazar por la consulta real con filtros y paginación (#150)
        var texto = filtros.Busqueda?.Trim();

        var filtradas = AsignacionesDeEjemplo.Todas
            .Where(a => (filtros.Estado is null || a.Estado == filtros.Estado)
                && (string.IsNullOrEmpty(texto)
                    || $"ASG-{a.NroAsignacion:D6}".Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || $"SOL-{a.NroSolicitud:D6}".Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || a.NroEquipo.ToString("D6").Contains(texto)
                    || a.MarcaYModelo.Contains(texto, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(a => a.Fecha)
            .ThenByDescending(a => a.NroAsignacion)
            .ToList();

        filtros.Total = filtradas.Count;
        filtros.Pagina = Math.Clamp(filtros.Pagina, 1, filtros.TotalPaginas);
        filtros.Filas = filtradas
            .Skip((filtros.Pagina - 1) * AsignacionListadoViewModel.TamanoPagina)
            .Take(AsignacionListadoViewModel.TamanoPagina)
            .Select(a => new AsignacionFilaViewModel
            {
                NroAsignacion = a.NroAsignacion,
                NroSolicitud = a.NroSolicitud,
                NroEquipo = a.NroEquipo,
                TipoEquipo = a.TipoEquipo,
                MarcaYModelo = a.MarcaYModelo,
                Coordinador = a.Coordinador,
                FechaAsignacion = a.Fecha,
                FechaLimiteRetiro = a.Limite,
                EstadoAsignacion = a.Estado
            })
            .ToList();

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public IActionResult Detalle(int id)
    {
        // TODO: reemplazar por la consulta real de la asignación (#150)
        var asignacion = AsignacionesDeEjemplo.Todas.FirstOrDefault(a => a.NroAsignacion == id);

        if (asignacion is null)
        {
            return NotFound();
        }

        return View(new AsignacionDetalleViewModel
        {
            NroAsignacion = asignacion.NroAsignacion,
            NroSolicitud = asignacion.NroSolicitud,
            Solicitante = asignacion.Solicitante,
            TipoSolicitante = asignacion.TipoSolicitante,
            NroEquipo = asignacion.NroEquipo,
            TipoEquipo = asignacion.TipoEquipo,
            MarcaYModelo = asignacion.MarcaYModelo,
            Coordinador = asignacion.Coordinador,
            FechaAsignacion = asignacion.Fecha,
            FechaLimiteRetiro = asignacion.Limite,
            EstadoAsignacion = asignacion.Estado,
            CambioPrioridad = asignacion.CambioPrioridad,
            JustificacionCambio = asignacion.Justificacion,
            Hitos = ArmarHitos(asignacion),
            Liberacion = new LiberarAsignacionViewModel { NroAsignacion = asignacion.NroAsignacion }
        });
    }

    [HttpGet]
    [Authorize(Policy = "SoloCoordinador")]
    public async Task<IActionResult> Asignar(int? nroEquipo)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var codigoPersonal = identityUserId is null ? null : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);
        var nombre = User.FindFirst(ClaimsSesion.Nombre)?.Value ?? string.Empty;

        var formulario = new AsignarEquipoViewModel
        {
            NroEquipo = nroEquipo,
            Coordinador = codigoPersonal is null ? nombre : $"{nombre} · PER-{codigoPersonal:D4}",
            FechaAsignacion = DateTime.Today,
            DiasLimiteRetiro = DiasLimiteRetiroDeEjemplo
        };

        if (nroEquipo is null)
        {
            var disponibles = await _donacionService.ObtenerDisponiblesAsync(null, null, null, OrdenCatalogo.Recientes, 1, 50);

            formulario.EquiposDisponibles = disponibles.Elementos
                .Select(e => (e.NroEquipo, $"{e.CodigoEquipo} · {e.MarcaYModelo}"))
                .ToList();

            return View(formulario);
        }

        var equipo = await _donacionService.ObtenerDetalleDisponibleAsync(nroEquipo.Value);

        if (equipo is null)
        {
            formulario.EquipoDisponible = false;
            return View(formulario);
        }

        formulario.TipoEquipo = equipo.TipoEquipo;
        formulario.MarcaYModelo = equipo.MarcaYModelo;
        formulario.Candidatas = CandidatasDeEjemplo(nroEquipo.Value);
        formulario.NroSolicitudSeleccionada = formulario.Candidatas.FirstOrDefault()?.NroSolicitud;

        return View(formulario);
    }

    // TODO: reemplazar por las solicitudes pendientes reales del equipo, ordenadas por la regla de prioridad (#122)
    private static IReadOnlyList<SolicitudCandidataViewModel> CandidatasDeEjemplo(int nroEquipo)
    {
        if (nroEquipo % 5 == 0)
        {
            return [];
        }

        var pendientes = new List<SolicitudCandidataViewModel>();

        if (nroEquipo % 2 == 0)
        {
            pendientes.Add(new() { NroSolicitud = 28, Solicitante = "Julieta Sosa", TipoSolicitante = TipoSolicitante.Particular, OrigenSolicitud = OrigenSolicitud.Autoservicio, FechaSolicitud = new DateTime(2026, 9, 1) });
            pendientes.Add(new() { NroSolicitud = 30, Solicitante = "Roberto Quiroga", TipoSolicitante = TipoSolicitante.OrganizacionSocial, OrigenSolicitud = OrigenSolicitud.Asistida, FechaSolicitud = new DateTime(2026, 9, 5) });
            pendientes.Add(new() { NroSolicitud = 29, Solicitante = "Laura Pereyra", TipoSolicitante = TipoSolicitante.Escuela, OrigenSolicitud = OrigenSolicitud.Autoservicio, FechaSolicitud = new DateTime(2026, 9, 3) });
        }
        else
        {
            pendientes.Add(new() { NroSolicitud = 31, Solicitante = "Valeria Acosta", TipoSolicitante = TipoSolicitante.Particular, OrigenSolicitud = OrigenSolicitud.Autoservicio, FechaSolicitud = new DateTime(2026, 9, 8) });
        }

        return pendientes
            .OrderBy(s => s.TipoSolicitante == TipoSolicitante.Particular ? 1 : 0)
            .ThenBy(s => s.FechaSolicitud)
            .ToList();
    }

    private static IReadOnlyList<HitoViewModel> ArmarHitos(AsignacionEjemplo asignacion)
    {
        var hitos = new List<HitoViewModel>
        {
            new()
            {
                Titulo = "Asignación registrada",
                Fecha = asignacion.Fecha,
                Icono = "swap",
                Estado = EstadoHito.Completado,
                Texto = $"Registrada por {asignacion.Coordinador}."
            }
        };

        switch (asignacion.Estado)
        {
            case EstadoAsignacion.Vigente:
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Plazo de retiro",
                    Icono = "clock",
                    Estado = EstadoHito.Actual,
                    Texto = $"Hasta el {asignacion.Limite:dd/MM/yyyy}."
                });
                hitos.Add(new HitoViewModel { Titulo = "Entrega del equipo", Icono = "truck", Estado = EstadoHito.Pendiente });
                break;

            case EstadoAsignacion.Concretada:
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Plazo de retiro",
                    Fecha = asignacion.Limite,
                    Icono = "clock",
                    Estado = EstadoHito.Completado
                });
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Entrega completada",
                    Fecha = asignacion.FechaEntrega,
                    Icono = "truck",
                    Estado = EstadoHito.Completado
                });
                break;

            default:
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Asignación liberada",
                    Fecha = asignacion.FechaLiberacion,
                    Icono = "undo",
                    Estado = EstadoHito.Cancelado,
                    Texto = asignacion.MotivoLiberacion
                });
                break;
        }

        return hitos;
    }
}
