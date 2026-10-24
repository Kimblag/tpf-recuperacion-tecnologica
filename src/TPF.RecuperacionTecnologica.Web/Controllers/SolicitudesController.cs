using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.Services.Solicitudes;
using TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class SolicitudesController : Controller
{
    private readonly IDonacionService _donacionService;
    private readonly IPersonalService _personalService;
    private readonly ISolicitudService _solicitudService;

    public SolicitudesController(
        IDonacionService donacionService,
        IPersonalService personalService,
        ISolicitudService solicitudService)
    {
        _donacionService = donacionService;
        _personalService = personalService;
        _solicitudService = solicitudService;
    }

    private sealed record SolicitudEjemplo(
        int NroSolicitud, int NroEquipo, TipoEquipo TipoEquipo, string MarcaYModelo, string Solicitante, bool Propia,
        TipoSolicitante TipoSolicitante, OrigenSolicitud OrigenSolicitud, DateTime Fecha,
        EstadoSolicitud Estado, string Motivo, DateTime? FechaAsignacion = null,
        DateTime? LimiteRetiro = null, DateTime? FechaEntrega = null);

    // TODO: reemplazar estos datos de ejemplo por las solicitudes reales (#149 y #151)
    private static readonly IReadOnlyList<SolicitudEjemplo> Ejemplos =
    [
        new(31, 126, TipoEquipo.Monitor, "Samsung S24F350", "Valeria Acosta", true, TipoSolicitante.Particular, OrigenSolicitud.Autoservicio,
            new DateTime(2026, 9, 8), EstadoSolicitud.Pendiente, "Necesito un monitor para el estudio de mi hija."),
        new(30, 123, TipoEquipo.Notebook, "Dell Latitude E6440", "Roberto Quiroga", false, TipoSolicitante.OrganizacionSocial, OrigenSolicitud.Asistida,
            new DateTime(2026, 9, 5), EstadoSolicitud.Pendiente, "Equipamiento para el taller de computación del comedor barrial."),
        new(29, 123, TipoEquipo.Notebook, "Dell Latitude E6440", "Laura Pereyra", false, TipoSolicitante.Escuela, OrigenSolicitud.Autoservicio,
            new DateTime(2026, 9, 3), EstadoSolicitud.Pendiente, "Renovar una computadora del laboratorio de informática."),
        new(28, 123, TipoEquipo.Notebook, "Dell Latitude E6440", "Julieta Sosa", false, TipoSolicitante.Particular, OrigenSolicitud.Autoservicio,
            new DateTime(2026, 9, 1), EstadoSolicitud.Pendiente, "Uso para estudiar y buscar trabajo."),
        new(27, 135, TipoEquipo.Celular, "Samsung Galaxy A10", "Marcela Bustos", false, TipoSolicitante.Escuela, OrigenSolicitud.Asistida,
            new DateTime(2026, 8, 30), EstadoSolicitud.Asignada, "Comunicación entre la escuela y las familias.",
            new DateTime(2026, 9, 2), new DateTime(2026, 9, 12)),
        new(25, 125, TipoEquipo.Notebook, "HP ProBook 450 G5", "Valeria Acosta", true, TipoSolicitante.Particular, OrigenSolicitud.Autoservicio,
            new DateTime(2026, 8, 28), EstadoSolicitud.Asignada, "Trabajo remoto y clases de mis hijos.",
            new DateTime(2026, 9, 15), new DateTime(2026, 9, 25)),
        new(20, 136, TipoEquipo.Monitor, "Dell E2216H", "Valeria Acosta", true, TipoSolicitante.Particular, OrigenSolicitud.Autoservicio,
            new DateTime(2026, 8, 20), EstadoSolicitud.Cancelada, "Ya no lo necesito."),
        new(19, 129, TipoEquipo.PcEscritorio, "Lenovo ThinkCentre M710", "Valeria Acosta", true, TipoSolicitante.Particular, OrigenSolicitud.Asistida,
            new DateTime(2026, 8, 12), EstadoSolicitud.Completada, "Computadora para el hogar.",
            new DateTime(2026, 8, 14), new DateTime(2026, 8, 24), new DateTime(2026, 8, 21)),
        new(17, 128, TipoEquipo.Periferico, "Logitech K120", "Valeria Acosta", true, TipoSolicitante.Particular, OrigenSolicitud.Autoservicio,
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
            .Where(s => s.Propia
                && (filtros.Estado is null || s.Estado == filtros.Estado)
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
            .Select(ArmarFila)
            .ToList();

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public IActionResult Detalle(int id)
    {
        // TODO: reemplazar por la consulta real, limitada al Usuario propietario (#149)
        var solicitud = Ejemplos.FirstOrDefault(s => s.Propia && s.NroSolicitud == id);

        return solicitud is null ? NotFound() : View(ArmarDetalle(solicitud));
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Registrar(int nroEquipo)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        var formulario = new FormularioSolicitudViewModel { NroEquipo = nroEquipo };

        return View(await CompletarDatosVisiblesAsync(formulario, identityUserId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Registrar(
        [Bind("NroEquipo,TipoSolicitante,MotivoSolicitud")] FormularioSolicitudViewModel formulario)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        await CompletarDatosVisiblesAsync(formulario, identityUserId);

        if (!formulario.EquipoDisponible || formulario.ImpedimentoMensaje is not null || !ModelState.IsValid)
        {
            return View(formulario);
        }

        var resultado = await _solicitudService.RegistrarAsync(identityUserId, formulario);

        if (resultado.Exitoso)
        {
            TempData["Mensaje"] = "La solicitud se registró correctamente.";
            return RedirectToAction(nameof(Index));
        }

        switch (resultado.Error)
        {
            case ErrorRegistroSolicitud.EquipoNoDisponible:
                formulario.EquipoDisponible = false;
                break;

            case ErrorRegistroSolicitud.TipoSolicitanteInvalido:
                ModelState.AddModelError(nameof(formulario.TipoSolicitante), resultado.Mensaje!);
                break;

            case ErrorRegistroSolicitud.Fallido:
                ModelState.AddModelError(string.Empty, resultado.Mensaje!);
                break;

            default:
                AplicarImpedimento(formulario, resultado.Error!.Value, resultado.Mensaje!);
                break;
        }

        return View(formulario);
    }

    private async Task<FormularioSolicitudViewModel> CompletarDatosVisiblesAsync(
        FormularioSolicitudViewModel formulario, string identityUserId)
    {
        var equipo = await _donacionService.ObtenerDetalleDisponibleAsync(formulario.NroEquipo);

        formulario.EquipoDisponible = equipo is not null;
        formulario.TipoEquipo = equipo?.TipoEquipo ?? TipoEquipo.Notebook;
        formulario.MarcaYModelo = equipo?.MarcaYModelo ?? string.Empty;
        formulario.Solicitante = User.FindFirst(ClaimsSesion.Nombre)?.Value ?? string.Empty;
        formulario.OrigenSolicitud = OrigenSolicitud.Autoservicio;
        formulario.FechaSolicitud = DateTime.Today;
        formulario.EstadoSolicitud = EstadoSolicitud.Pendiente;
        formulario.TiposPermitidos = TiposPermitidosDeLaSesion();

        if (formulario.TiposPermitidos.Count == 1)
        {
            formulario.TipoSolicitante ??= formulario.TiposPermitidos[0];
        }

        if (equipo is not null)
        {
            var impedimento = await _solicitudService.ObtenerImpedimentoAsync(identityUserId, formulario.NroEquipo);

            if (impedimento is not null)
            {
                AplicarImpedimento(formulario, impedimento.Error, impedimento.Mensaje);
            }
        }

        return formulario;
    }

    private static void AplicarImpedimento(FormularioSolicitudViewModel formulario, ErrorRegistroSolicitud error, string mensaje)
    {
        formulario.ImpedimentoTitulo = error switch
        {
            ErrorRegistroSolicitud.SolicitudDuplicada => "Ya hay una solicitud pendiente para este equipo",
            ErrorRegistroSolicitud.EquipoPropio => "Este equipo no se puede solicitar",
            ErrorRegistroSolicitud.LimiteAlcanzado => "Se alcanzó el máximo de solicitudes pendientes",
            _ => "No se puede registrar la solicitud"
        };
        formulario.ImpedimentoMensaje = mensaje;
    }

    private IReadOnlyList<TipoSolicitante> TiposPermitidosDeLaSesion()
    {
        var esInstitucion = Enum.TryParse<TipoUsuario>(User.FindFirstValue(ClaimsSesion.TipoUsuario), out var tipoUsuario)
            && tipoUsuario == TipoUsuario.Institucion;

        if (esInstitucion)
        {
            return [TipoSolicitante.Escuela, TipoSolicitante.OrganizacionSocial];
        }

        return [TipoSolicitante.Particular];
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public IActionResult Editar(int id)
    {
        // TODO: reemplazar por la consulta real de la solicitud propia (#119)
        var solicitud = Ejemplos.FirstOrDefault(s => s.Propia && s.NroSolicitud == id);

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
            .Where(s => s.Propia)
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

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public IActionResult Gestion(SolicitudListadoViewModel filtros)
    {
        // TODO: reemplazar por la consulta real con filtros y paginación (#151)
        var texto = filtros.Busqueda?.Trim();

        var filtradas = Ejemplos
            .Where(s => (filtros.Tipo is null || s.TipoSolicitante == filtros.Tipo)
                && (filtros.Origen is null || s.OrigenSolicitud == filtros.Origen)
                && (filtros.Estado is null || s.Estado == filtros.Estado)
                && (string.IsNullOrEmpty(texto)
                    || $"SOL-{s.NroSolicitud:D6}".Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || s.NroEquipo.ToString("D6").Contains(texto)
                    || s.Solicitante.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || s.MarcaYModelo.Contains(texto, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(s => s.Fecha)
            .ThenByDescending(s => s.NroSolicitud)
            .ToList();

        filtros.Total = filtradas.Count;
        filtros.Pagina = Math.Clamp(filtros.Pagina, 1, filtros.TotalPaginas);
        filtros.Filas = filtradas
            .Skip((filtros.Pagina - 1) * SolicitudListadoViewModel.TamanoPagina)
            .Take(SolicitudListadoViewModel.TamanoPagina)
            .Select(ArmarFila)
            .ToList();

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public IActionResult DetalleInterno(int id)
    {
        // TODO: reemplazar por la consulta real de cualquier solicitud (#151)
        var solicitud = Ejemplos.FirstOrDefault(s => s.NroSolicitud == id);

        return solicitud is null ? NotFound() : View(ArmarDetalle(solicitud));
    }

    [HttpGet]
    [Authorize(Policy = "SoloCoordinador")]
    public async Task<IActionResult> RegistrarAsistida(ModoUsuarioAsistido modo, TipoUsuario tipoUsuario, string? busqueda)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var codigoPersonal = identityUserId is null ? null : await _personalService.ObtenerCodigoPersonalAsync(identityUserId);
        var nombre = User.FindFirst(ClaimsSesion.Nombre)?.Value ?? string.Empty;

        var formulario = new RegistroSolicitudAsistidaViewModel
        {
            Usuario = new SelectorUsuarioViewModel { Modo = modo, TipoUsuario = tipoUsuario, Busqueda = busqueda?.Trim() },
            Coordinador = codigoPersonal is null ? nombre : $"{nombre} · PER-{codigoPersonal:D4}",
            FechaSolicitud = DateTime.Today
        };

        var disponibles = await _donacionService.ObtenerDisponiblesAsync(null, null, null, OrdenCatalogo.Recientes, 1, 50);

        formulario.EquiposDisponibles = disponibles.Elementos
            .Select(e => (e.NroEquipo, $"{e.CodigoEquipo} · {e.MarcaYModelo}"))
            .ToList();

        UsuariosDeEjemplo.Buscar(formulario.Usuario);

        return View(formulario);
    }

    private static SolicitudFilaViewModel ArmarFila(SolicitudEjemplo solicitud) => new()
    {
        NroSolicitud = solicitud.NroSolicitud,
        NroEquipo = solicitud.NroEquipo,
        TipoEquipo = solicitud.TipoEquipo,
        MarcaYModelo = solicitud.MarcaYModelo,
        Solicitante = solicitud.Solicitante,
        TipoSolicitante = solicitud.TipoSolicitante,
        OrigenSolicitud = solicitud.OrigenSolicitud,
        FechaSolicitud = solicitud.Fecha,
        EstadoSolicitud = solicitud.Estado
    };

    private static SolicitudDetalleViewModel ArmarDetalle(SolicitudEjemplo solicitud) => new()
    {
        NroSolicitud = solicitud.NroSolicitud,
        NroEquipo = solicitud.NroEquipo,
        TipoEquipo = solicitud.TipoEquipo,
        MarcaYModelo = solicitud.MarcaYModelo,
        Solicitante = solicitud.Solicitante,
        TipoSolicitante = solicitud.TipoSolicitante,
        MotivoSolicitud = solicitud.Motivo,
        OrigenSolicitud = solicitud.OrigenSolicitud,
        FechaSolicitud = solicitud.Fecha,
        EstadoSolicitud = solicitud.Estado,
        Hitos = ArmarHitos(solicitud)
    };

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
