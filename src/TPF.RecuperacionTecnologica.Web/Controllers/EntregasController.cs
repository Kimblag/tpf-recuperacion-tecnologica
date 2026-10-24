using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Entregas;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class EntregasController : Controller
{
    private const int MaximoCandidatas = 10;

    private readonly IPersonalService _personalService;

    public EntregasController(IPersonalService personalService)
    {
        _personalService = personalService;
    }

    private sealed record EntregaEjemplo(AsignacionEjemplo Asignacion, int NroEntrega, string QuienRetira, string Documento);

    // TODO: reemplazar estos datos de ejemplo por las entregas reales (#152)
    private static readonly IReadOnlyDictionary<int, (int NroEntrega, string Nombre, string Documento)> DatosDeEntrega =
        new Dictionary<int, (int NroEntrega, string Nombre, string Documento)>
        {
            [16] = (12, "Valeria Acosta", "34205918"),
            [14] = (11, "Marcos Ibarra", "29771406"),
            [13] = (10, "Daniela Ríos", "40118552")
        };

    private static readonly IReadOnlyList<EntregaEjemplo> Ejemplos = AsignacionesDeEjemplo.Todas
        .Where(a => DatosDeEntrega.ContainsKey(a.NroAsignacion))
        .Select(a => new EntregaEjemplo(
            a,
            DatosDeEntrega[a.NroAsignacion].NroEntrega,
            DatosDeEntrega[a.NroAsignacion].Nombre,
            DatosDeEntrega[a.NroAsignacion].Documento))
        .ToList();

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public IActionResult Index(EntregaListadoViewModel filtros)
    {
        // TODO: reemplazar por la consulta real con filtros y paginación (#152)
        var texto = filtros.Busqueda?.Trim();

        var filtradas = Ejemplos
            .Where(e => (filtros.FechaDesde is null || e.Asignacion.FechaEntrega!.Value.Date >= filtros.FechaDesde.Value.Date)
                && (filtros.FechaHasta is null || e.Asignacion.FechaEntrega!.Value.Date <= filtros.FechaHasta.Value.Date)
                && (string.IsNullOrEmpty(texto)
                    || $"ENT-{e.NroEntrega:D6}".Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || $"ASG-{e.Asignacion.NroAsignacion:D6}".Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || $"SOL-{e.Asignacion.NroSolicitud:D6}".Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || e.Asignacion.NroEquipo.ToString("D6").Contains(texto)
                    || e.Asignacion.MarcaYModelo.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || e.QuienRetira.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || e.Documento.Contains(texto)))
            .OrderByDescending(e => e.Asignacion.FechaEntrega)
            .ThenByDescending(e => e.NroEntrega)
            .ToList();

        filtros.Total = filtradas.Count;
        filtros.Pagina = Math.Clamp(filtros.Pagina, 1, filtros.TotalPaginas);
        filtros.Filas = filtradas
            .Skip((filtros.Pagina - 1) * EntregaListadoViewModel.TamanoPagina)
            .Take(EntregaListadoViewModel.TamanoPagina)
            .Select(ArmarFila)
            .ToList();

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public IActionResult Detalle(int id)
    {
        // TODO: reemplazar por la consulta real de la entrega (#152)
        var entrega = Ejemplos.FirstOrDefault(e => e.NroEntrega == id);

        if (entrega is null)
        {
            return NotFound();
        }

        var asignacion = entrega.Asignacion;

        return View(new EntregaDetalleViewModel
        {
            NroEntrega = entrega.NroEntrega,
            NroAsignacion = asignacion.NroAsignacion,
            NroSolicitud = asignacion.NroSolicitud,
            NroEquipo = asignacion.NroEquipo,
            TipoEquipo = asignacion.TipoEquipo,
            MarcaYModelo = asignacion.MarcaYModelo,
            Solicitante = asignacion.Solicitante,
            TipoSolicitante = asignacion.TipoSolicitante,
            Coordinador = asignacion.Coordinador,
            FechaAsignacion = asignacion.Fecha,
            FechaLimiteRetiro = asignacion.Limite,
            FechaEntrega = asignacion.FechaEntrega!.Value,
            NombreQuienRetira = entrega.QuienRetira,
            DocumentoQuienRetira = entrega.Documento,
            Hitos = ArmarHitos(entrega)
        });
    }

    [HttpGet]
    [Authorize(Policy = "SoloCoordinador")]
    public async Task<IActionResult> Registrar(int? nroAsignacion, string? busqueda)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var codigoPersonal = identityUserId is null ? null : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);
        var nombre = User.FindFirst(ClaimsSesion.Nombre)?.Value ?? string.Empty;

        var formulario = new RegistroEntregaViewModel
        {
            NroAsignacion = nroAsignacion,
            Busqueda = busqueda?.Trim(),
            Coordinador = codigoPersonal is null ? nombre : $"{nombre} · PER-{codigoPersonal:D4}",
            FechaEntrega = DateTime.Today
        };

        if (nroAsignacion is null)
        {
            formulario.Situacion = SituacionEntrega.ElegirAsignacion;
            formulario.Candidatas = BuscarVigentes(formulario.Busqueda);

            return View(formulario);
        }

        // TODO: reemplazar por la consulta real de la asignación (#124)
        var asignacion = AsignacionesDeEjemplo.Todas.FirstOrDefault(a => a.NroAsignacion == nroAsignacion);

        if (asignacion is null)
        {
            formulario.Situacion = SituacionEntrega.NoEncontrada;

            return View(formulario);
        }

        formulario.EstadoAsignacion = asignacion.Estado;
        formulario.NroSolicitud = asignacion.NroSolicitud;
        formulario.Solicitante = asignacion.Solicitante;
        formulario.TipoSolicitante = asignacion.TipoSolicitante;
        formulario.NroEquipo = asignacion.NroEquipo;
        formulario.TipoEquipo = asignacion.TipoEquipo;
        formulario.MarcaYModelo = asignacion.MarcaYModelo;
        formulario.FechaLimiteRetiro = asignacion.Limite;

        formulario.Situacion = asignacion.Estado != EstadoAsignacion.Vigente
            ? SituacionEntrega.NoVigente
            : asignacion.Limite.Date < DateTime.Today
                ? SituacionEntrega.PlazoVencido
                : SituacionEntrega.Lista;

        return View(formulario);
    }

    private static IReadOnlyList<AsignacionVigenteFilaViewModel> BuscarVigentes(string? busqueda)
    {
        return AsignacionesDeEjemplo.Todas
            .Where(a => a.Estado == EstadoAsignacion.Vigente
                && (string.IsNullOrEmpty(busqueda)
                    || $"ASG-{a.NroAsignacion:D6}".Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                    || $"SOL-{a.NroSolicitud:D6}".Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                    || a.NroEquipo.ToString("D6").Contains(busqueda)
                    || a.MarcaYModelo.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                    || a.Solicitante.Contains(busqueda, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(a => a.Limite)
            .Take(MaximoCandidatas)
            .Select(a => new AsignacionVigenteFilaViewModel
            {
                NroAsignacion = a.NroAsignacion,
                NroSolicitud = a.NroSolicitud,
                NroEquipo = a.NroEquipo,
                TipoEquipo = a.TipoEquipo,
                MarcaYModelo = a.MarcaYModelo,
                Solicitante = a.Solicitante,
                FechaLimiteRetiro = a.Limite,
                PlazoVencido = a.Limite.Date < DateTime.Today
            })
            .ToList();
    }

    private static EntregaFilaViewModel ArmarFila(EntregaEjemplo entrega) => new()
    {
        NroEntrega = entrega.NroEntrega,
        NroAsignacion = entrega.Asignacion.NroAsignacion,
        NroSolicitud = entrega.Asignacion.NroSolicitud,
        NroEquipo = entrega.Asignacion.NroEquipo,
        TipoEquipo = entrega.Asignacion.TipoEquipo,
        MarcaYModelo = entrega.Asignacion.MarcaYModelo,
        Solicitante = entrega.Asignacion.Solicitante,
        Coordinador = entrega.Asignacion.Coordinador,
        FechaEntrega = entrega.Asignacion.FechaEntrega!.Value,
        NombreQuienRetira = entrega.QuienRetira,
        DocumentoQuienRetira = entrega.Documento
    };

    private static IReadOnlyList<HitoViewModel> ArmarHitos(EntregaEjemplo entrega) =>
    [
        new HitoViewModel
        {
            Titulo = "Asignación registrada",
            Fecha = entrega.Asignacion.Fecha,
            Icono = "swap",
            Estado = EstadoHito.Completado,
            Texto = $"Registrada por {entrega.Asignacion.Coordinador}."
        },
        new HitoViewModel
        {
            Titulo = "Plazo de retiro",
            Fecha = entrega.Asignacion.Limite,
            Icono = "clock",
            Estado = EstadoHito.Completado,
            Texto = "Fecha límite para retirar el equipo."
        },
        new HitoViewModel
        {
            Titulo = "Entrega completada",
            Fecha = entrega.Asignacion.FechaEntrega,
            Icono = "truck",
            Estado = EstadoHito.Completado,
            Texto = $"Retiró {entrega.QuienRetira}."
        }
    ];
}
