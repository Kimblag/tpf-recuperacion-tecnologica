using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Asignaciones;
using TPF.RecuperacionTecnologica.Web.Services.Configuracion;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.ViewModels.Asignaciones;
using TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class AsignacionesController : Controller
{
    // se usa solo si todavía no existe la fila de configuración
    private const int DiasLimiteRetiroPorDefecto = 10;

    private readonly IDonacionService _donacionService;
    private readonly IPersonalService _personalService;
    private readonly IConfiguracionService _configuracionService;
    private readonly IAsignacionService _asignacionService;

    public AsignacionesController(
        IDonacionService donacionService,
        IPersonalService personalService,
        IConfiguracionService configuracionService,
        IAsignacionService asignacionService)
    {
        _donacionService = donacionService;
        _personalService = personalService;
        _configuracionService = configuracionService;
        _asignacionService = asignacionService;
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
    public async Task<IActionResult> Asignar(int? nroEquipo, string? busquedaEquipo)
    {
        var formulario = new AsignarEquipoViewModel { NroEquipo = nroEquipo };
        formulario.Equipo.Busqueda = busquedaEquipo;

        await CompletarFormularioAsync(formulario);

        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SoloCoordinador")]
    public async Task<IActionResult> Asignar(
        [Bind("NroEquipo,NroSolicitudSeleccionada,JustificacionCambio")] AsignarEquipoViewModel formulario)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        if (formulario.NroEquipo is not null && ModelState.IsValid)
        {
            var resultado = await _asignacionService.AsignarAsync(
                identityUserId, formulario.NroEquipo.Value, formulario.NroSolicitudSeleccionada!.Value, formulario.JustificacionCambio);

            if (resultado.Exitoso)
            {
                TempData["Mensaje"] = "La asignación se registró correctamente.";
                return RedirectToAction(nameof(Detalle), new { id = resultado.NroAsignacion });
            }

            ModelState.AddModelError(string.Empty, resultado.Mensaje!);
        }

        await CompletarFormularioAsync(formulario);

        return View(formulario);
    }

    private const int MaximoResultadosEquipo = 10;

    private async Task CompletarFormularioAsync(AsignarEquipoViewModel formulario)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var codigoPersonal = identityUserId is null ? null : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);
        var nombre = User.FindFirst(ClaimsSesion.Nombre)?.Value ?? string.Empty;
        var diasLimiteRetiro = (await _configuracionService.ObtenerAsync())?.DiasLimiteRetiro
            ?? DiasLimiteRetiroPorDefecto;

        formulario.Coordinador = codigoPersonal is null ? nombre : $"{nombre} · PER-{codigoPersonal:D4}";
        formulario.FechaAsignacion = DateTime.Today;
        formulario.DiasLimiteRetiro = diasLimiteRetiro;

        if (formulario.NroEquipo is null)
        {
            var selector = formulario.Equipo;

            // solo se ofrecen equipos con solicitudes pendientes; sin búsqueda se precargan los más recientes
            selector.BusquedaRealizada = true;
            selector.Resultados = await _asignacionService.ObtenerEquiposConSolicitudPendienteAsync(
                selector.Busqueda, MaximoResultadosEquipo);

            return;
        }

        var equipo = await _donacionService.ObtenerDetalleDisponibleAsync(formulario.NroEquipo.Value);

        if (equipo is null)
        {
            formulario.EquipoDisponible = false;
            return;
        }

        formulario.TipoEquipo = equipo.TipoEquipo;
        formulario.MarcaYModelo = equipo.MarcaYModelo;
        formulario.Candidatas = await _asignacionService.ObtenerCandidatasAsync(formulario.NroEquipo.Value);

        // si no llegó una elección válida, se preselecciona la sugerida
        if (formulario.NroSolicitudSeleccionada is null
            || formulario.Candidatas.All(c => c.NroSolicitud != formulario.NroSolicitudSeleccionada))
        {
            formulario.NroSolicitudSeleccionada = formulario.Candidatas.FirstOrDefault()?.NroSolicitud;
        }
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
