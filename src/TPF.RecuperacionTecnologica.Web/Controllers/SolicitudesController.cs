using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.Services.Solicitudes;
using TPF.RecuperacionTecnologica.Web.Services.Usuarios;
using TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class SolicitudesController : Controller
{
    private readonly IDonacionService _donacionService;
    private readonly IPersonalService _personalService;
    private readonly ISolicitudService _solicitudService;
    private readonly ISolicitudConsultaService _consultaService;
    private readonly IUsuarioService _usuarioService;
    private readonly ILogger<SolicitudesController> _logger;

    public SolicitudesController(
        IDonacionService donacionService,
        IPersonalService personalService,
        ISolicitudService solicitudService,
        ISolicitudConsultaService consultaService,
        IUsuarioService usuarioService,
        ILogger<SolicitudesController> logger)
    {
        _donacionService = donacionService;
        _personalService = personalService;
        _solicitudService = solicitudService;
        _consultaService = consultaService;
        _usuarioService = usuarioService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Index(MisSolicitudesViewModel filtros)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        try
        {
            var resultado = await _consultaService.ListarPropiasAsync(
                identityUserId, filtros.Busqueda, filtros.Estado, filtros.Pagina, MisSolicitudesViewModel.TamanoPagina);

            filtros.Filas = resultado.Elementos;
            filtros.Total = resultado.Total;
            filtros.Pagina = resultado.Pagina;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el listado de solicitudes propias");
            filtros.ErrorAlCargar = true;
        }

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Detalle(int id)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        var detalle = await _consultaService.ObtenerDetallePropioAsync(identityUserId, id);

        return detalle is null ? NotFound() : View(detalle);
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

    private static string TituloDelImpedimento(ErrorRegistroSolicitud error) => error switch
    {
        ErrorRegistroSolicitud.SolicitudDuplicada => "Ya hay una solicitud pendiente para este equipo",
        ErrorRegistroSolicitud.EquipoPropio => "Este equipo no se puede solicitar",
        ErrorRegistroSolicitud.LimiteAlcanzado => "Se alcanzó el máximo de solicitudes pendientes",
        ErrorRegistroSolicitud.UsuarioNoHabilitado => "El usuario no está habilitado",
        _ => "No se puede registrar la solicitud"
    };

    private static void AplicarImpedimento(FormularioSolicitudViewModel formulario, ErrorRegistroSolicitud error, string mensaje)
    {
        formulario.ImpedimentoTitulo = TituloDelImpedimento(error);
        formulario.ImpedimentoMensaje = mensaje;
    }

    private async Task AplicarImpedimentoAsistidoAsync(RegistroSolicitudAsistidaViewModel formulario)
    {
        if (formulario.Usuario.Modo != ModoUsuarioAsistido.Existente
            || formulario.Usuario.NroUsuario is null
            || formulario.NroEquipo is null)
        {
            return;
        }

        var impedimento = await _solicitudService.ObtenerImpedimentoParaUsuarioAsync(
            formulario.Usuario.NroUsuario.Value, formulario.NroEquipo.Value);

        if (impedimento is not null)
        {
            formulario.ImpedimentoTitulo = TituloDelImpedimento(impedimento.Error);
            formulario.ImpedimentoMensaje = impedimento.Mensaje;
        }
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
    public async Task<IActionResult> Editar(int id)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        var formulario = await _consultaService.ObtenerParaEdicionPropiaAsync(identityUserId, id);

        return formulario is null ? NotFound() : View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Editar(int id, [Bind("MotivoSolicitud")] EditarMotivoSolicitudViewModel formulario)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        if (ModelState.IsValid)
        {
            var resultado = await _solicitudService.EditarMotivoAsync(identityUserId, id, formulario.MotivoSolicitud);

            if (resultado.Succeeded)
            {
                TempData["Mensaje"] = "El motivo de la solicitud se actualizó correctamente.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            if (resultado.Errors.Any(e => e.Code == "SolicitudNoEncontrada"))
            {
                return NotFound();
            }

            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        var actual = await _consultaService.ObtenerParaEdicionPropiaAsync(identityUserId, id);

        if (actual is null)
        {
            return NotFound();
        }

        actual.MotivoSolicitud = formulario.MotivoSolicitud;

        return View(actual);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Cancelar(int id)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        var original = await _consultaService.ObtenerDetallePropioAsync(identityUserId, id);

        if (original is null)
        {
            return NotFound();
        }

        var resultado = await _solicitudService.CancelarAsync(identityUserId, id);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            var actual = await _consultaService.ObtenerDetallePropioAsync(identityUserId, id);

            return View(nameof(Detalle), actual ?? original);
        }

        TempData["Mensaje"] = "La solicitud se canceló correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public async Task<IActionResult> Gestion(SolicitudListadoViewModel filtros)
    {
        try
        {
            var resultado = await _consultaService.ListarTodasAsync(
                filtros.Busqueda, filtros.Tipo, filtros.Origen, filtros.Estado,
                filtros.Pagina, SolicitudListadoViewModel.TamanoPagina);

            filtros.Filas = resultado.Elementos;
            filtros.Total = resultado.Total;
            filtros.Pagina = resultado.Pagina;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el listado de solicitudes");
            filtros.ErrorAlCargar = true;
        }

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public async Task<IActionResult> DetalleInterno(int id)
    {
        var detalle = await _consultaService.ObtenerDetalleInternoAsync(id);

        return detalle is null ? NotFound() : View(detalle);
    }

    [HttpGet]
    [Authorize(Policy = "SoloCoordinador")]
    public async Task<IActionResult> RegistrarAsistida(
        ModoUsuarioAsistido modo, TipoUsuario tipoUsuario, string? busqueda, string? busquedaEquipo, int? nroEquipo)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        var formulario = new RegistroSolicitudAsistidaViewModel
        {
            Usuario = new SelectorUsuarioViewModel { Modo = modo, TipoUsuario = tipoUsuario, Busqueda = busqueda?.Trim() },
            NroEquipo = nroEquipo
        };

        formulario.Equipo.Busqueda = busquedaEquipo?.Trim();

        await CompletarFormularioAsistidoAsync(formulario, identityUserId);

        if (modo == ModoUsuarioAsistido.Existente && !string.IsNullOrWhiteSpace(formulario.Usuario.Busqueda))
        {
            var encontrado = await _usuarioService.BuscarPorDocumentoOCorreoAsync(formulario.Usuario.Busqueda);

            AplicarUsuarioEncontrado(formulario.Usuario, encontrado);
            formulario.Usuario.BusquedaRealizada = true;
        }

        await AplicarImpedimentoAsistidoAsync(formulario);

        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SoloCoordinador")]
    public async Task<IActionResult> RegistrarAsistida(
        [Bind("Usuario,TipoSolicitante,NroEquipo,MotivoSolicitud")] RegistroSolicitudAsistidaViewModel formulario)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        if (ModelState.IsValid)
        {
            var resultado = await _solicitudService.RegistrarAsistidaAsync(
                identityUserId,
                formulario,
                (idUsuario, token) => Url.Action("RestablecerContrasena", "Cuenta", new { idUsuario, token })!,
                $"{Request.Scheme}://{Request.Host}");

            if (resultado.Exitoso)
            {
                TempData["Mensaje"] = formulario.Usuario.Modo == ModoUsuarioAsistido.Nuevo
                    ? $"La solicitud asistida se registró y se envió una invitación a {formulario.Usuario.CorreoElectronico!.Trim()} para que la persona defina su contraseña."
                    : "La solicitud asistida se registró correctamente.";

                return RedirectToAction(nameof(Gestion));
            }

            ModelState.AddModelError(string.Empty, resultado.Mensaje!);
        }

        await CompletarFormularioAsistidoAsync(formulario, identityUserId);

        if (formulario.Usuario.Modo == ModoUsuarioAsistido.Existente && formulario.Usuario.NroUsuario is not null)
        {
            var encontrado = await _usuarioService.ObtenerUsuarioEncontradoAsync(formulario.Usuario.NroUsuario.Value);

            AplicarUsuarioEncontrado(formulario.Usuario, encontrado);
            formulario.Usuario.BusquedaRealizada = true;
            ModelState.Remove("Usuario.NroUsuario");
        }

        await AplicarImpedimentoAsistidoAsync(formulario);

        return View(formulario);
    }

    private const int MaximoResultadosEquipo = 10;

    private async Task CompletarFormularioAsistidoAsync(RegistroSolicitudAsistidaViewModel formulario, string identityUserId)
    {
        var codigoPersonal = await _personalService.ObtenerCodigoPersonalAsync(identityUserId);
        var nombre = User.FindFirst(ClaimsSesion.Nombre)?.Value ?? string.Empty;

        formulario.Coordinador = codigoPersonal is null ? nombre : $"{nombre} · PER-{codigoPersonal:D4}";
        formulario.FechaSolicitud = DateTime.Today;

        var selector = formulario.Equipo;

        if (formulario.NroEquipo is not null)
        {
            var equipo = await _donacionService.ObtenerDetalleDisponibleAsync(formulario.NroEquipo.Value);

            if (equipo is null)
            {
                formulario.NroEquipo = null;
                ModelState.Remove(nameof(formulario.NroEquipo));
            }
            else
            {
                selector.Seleccionado = new EquipoSeleccionableViewModel
                {
                    NroEquipo = equipo.NroEquipo,
                    TipoEquipo = equipo.TipoEquipo,
                    MarcaYModelo = equipo.MarcaYModelo
                };
            }
        }

        if (selector.Seleccionado is null && !string.IsNullOrWhiteSpace(selector.Busqueda))
        {
            var resultado = await _donacionService.ObtenerDisponiblesAsync(
                selector.Busqueda, null, null, OrdenCatalogo.Recientes, 1, MaximoResultadosEquipo);

            selector.BusquedaRealizada = true;
            selector.Resultados = resultado.Elementos
                .Select(e => new EquipoSeleccionableViewModel
                {
                    NroEquipo = e.NroEquipo,
                    TipoEquipo = e.TipoEquipo,
                    MarcaYModelo = e.MarcaYModelo
                })
                .ToList();
        }
    }

    private static void AplicarUsuarioEncontrado(SelectorUsuarioViewModel selector, UsuarioEncontradoViewModel? encontrado)
    {
        selector.Encontrado = encontrado;
        selector.NroUsuario = encontrado is { EstadoUsuario: EstadoUsuario.Activo } ? encontrado.NroUsuario : null;
    }
}
