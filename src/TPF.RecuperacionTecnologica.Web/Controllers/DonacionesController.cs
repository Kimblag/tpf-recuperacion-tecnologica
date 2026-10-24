using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Donaciones;
using TPF.RecuperacionTecnologica.Web.Services.Personal;
using TPF.RecuperacionTecnologica.Web.Services.Usuarios;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

public class DonacionesController : Controller
{
    private readonly IDonacionService _donacionService;
    private readonly IPersonalService _personalService;
    private readonly IUsuarioService _usuarioService;
    private readonly ILogger<DonacionesController> _logger;

    public DonacionesController(
        IDonacionService donacionService,
        IPersonalService personalService,
        IUsuarioService usuarioService,
        ILogger<DonacionesController> logger)
    {
        _donacionService = donacionService;
        _personalService = personalService;
        _usuarioService = usuarioService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = "SoloCoordinador")]
    public async Task<IActionResult> RegistrarAsistida(ModoUsuarioAsistido modo, TipoUsuario tipoUsuario, string? busqueda)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        var formulario = new RegistroDonacionAsistidaViewModel
        {
            Usuario = new SelectorUsuarioViewModel { Modo = modo, TipoUsuario = tipoUsuario, Busqueda = busqueda?.Trim() }
        };

        await CompletarFormularioAsistidoAsync(formulario, identityUserId);

        if (modo == ModoUsuarioAsistido.Existente && !string.IsNullOrWhiteSpace(formulario.Usuario.Busqueda))
        {
            var encontrado = await _usuarioService.BuscarPorDocumentoOCorreoAsync(formulario.Usuario.Busqueda);

            AplicarUsuarioEncontrado(formulario.Usuario, encontrado);
            formulario.Usuario.BusquedaRealizada = true;
        }

        return View(formulario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "SoloCoordinador")]
    public async Task<IActionResult> RegistrarAsistida(
        [Bind("Usuario,TipoEquipo,Marca,Modelo,EstadoInicialDeclarado,Observaciones,Procesador,MemoriaRam,Almacenamiento,TamanoPantalla,EstadoBateria,Otros")]
        RegistroDonacionAsistidaViewModel formulario)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return Challenge();
        }

        if (ModelState.IsValid)
        {
            var resultado = await _donacionService.RegistrarAsistidaAsync(
                identityUserId,
                formulario,
                (idUsuario, token) => Url.Action("RestablecerContrasena", "Cuenta", new { idUsuario, token })!,
                $"{Request.Scheme}://{Request.Host}");

            if (resultado.Exitoso)
            {
                TempData["Mensaje"] = formulario.Usuario.Modo == ModoUsuarioAsistido.Nuevo
                    ? $"La donación asistida se registró y se envió una invitación a {formulario.Usuario.CorreoElectronico!.Trim()} para que la persona defina su contraseña. Estado: Pendiente de recepción."
                    : "La donación asistida se registró correctamente. Estado: Pendiente de recepción.";

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

        return View(formulario);
    }

    private async Task CompletarFormularioAsistidoAsync(RegistroDonacionAsistidaViewModel formulario, string identityUserId)
    {
        var codigoPersonal = await _personalService.ObtenerCodigoPersonalAsync(identityUserId);
        var nombre = User.FindFirst(ClaimsSesion.Nombre)?.Value ?? string.Empty;

        formulario.Coordinador = codigoPersonal is null ? nombre : $"{nombre} · PER-{codigoPersonal:D4}";
    }

    private static void AplicarUsuarioEncontrado(SelectorUsuarioViewModel selector, UsuarioEncontradoViewModel? encontrado)
    {
        selector.Encontrado = encontrado;
        selector.NroUsuario = encontrado is { EstadoUsuario: EstadoUsuario.Activo } ? encontrado.NroUsuario : null;
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Index(MisDonacionesViewModel filtros)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identityUserId is null)
        {
            return NotFound();
        }

        try
        {
            var resultado = await _donacionService.ListarAsync(
                identityUserId, filtros.Busqueda, filtros.Estado, filtros.Tipo, filtros.Condicion,
                filtros.Orden, filtros.Pagina, MisDonacionesViewModel.TamanoPagina);

            filtros.Filas = resultado.Elementos;
            filtros.Total = resultado.Total;
            filtros.Pagina = resultado.Pagina;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el listado de donaciones");
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
            return NotFound();
        }

        try
        {
            var donacion = await _donacionService.ObtenerDetalleAsync(identityUserId, id);

            return donacion is null ? NotFound() : View(donacion);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el detalle de la donación {NroEquipo}", id);
            return View("DetalleError");
        }
    }

    // listado de todas las donaciones para el personal interno; el donante usa Index y Detalle
    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public async Task<IActionResult> Gestion(DonacionListadoViewModel filtros)
    {
        try
        {
            var resultado = await _donacionService.ListarTodasAsync(
                filtros.Busqueda, filtros.Donante, filtros.Estado, filtros.Pagina, DonacionListadoViewModel.TamanoPagina);

            filtros.Filas = resultado.Elementos;
            filtros.Total = resultado.Total;
            filtros.Pagina = resultado.Pagina;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el listado de donaciones del personal");
            filtros.ErrorAlCargar = true;
        }

        return View(filtros);
    }

    [HttpGet]
    [Authorize(Policy = "Coordinacion")]
    public async Task<IActionResult> DetalleInterno(int id)
    {
        try
        {
            var donacion = await _donacionService.ObtenerDetalleInternoAsync(id);

            return donacion is null ? NotFound() : View(donacion);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo cargar el detalle de la donación {NroEquipo}", id);
            return View("DetalleInternoError");
        }
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public IActionResult Registrar()
    {
        return View(new RegistroDonacionViewModel());
    }

    // el donante se identifica por la sesión, no por un dato del formulario.
    // el parámetro no se llama "modelo" porque el ViewModel tiene la propiedad Modelo y confundiría al model binding
    [HttpPost]
    [Authorize(Policy = "UsuarioExterno")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(RegistroDonacionViewModel donacion)
    {
        if (!ModelState.IsValid)
        {
            return View(donacion);
        }

        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var nroEquipo = identityUserId is null ? null : await _donacionService.RegistrarAsync(identityUserId, donacion);

        return nroEquipo is null
            ? NotFound()
            : RedirectToAction(nameof(Confirmacion), new { id = nroEquipo });
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Confirmacion(int id)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var donacion = identityUserId is null
            ? null
            : await _donacionService.ObtenerDetalleAsync(identityUserId, id);

        return donacion is null ? NotFound() : View(id);
    }

    [HttpGet]
    [Authorize(Policy = "UsuarioExterno")]
    public async Task<IActionResult> Editar(int id)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var donacion = identityUserId is null
            ? null
            : await _donacionService.ObtenerParaEdicionAsync(identityUserId, id);

        // una donación inexistente y una de otro Usuario responden igual
        if (donacion is null)
        {
            return NotFound();
        }

        return donacion.EstadoEquipo == EstadoEquipo.PendienteRecepcion
            ? View(donacion)
            : RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [Authorize(Policy = "UsuarioExterno")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EdicionDonacionViewModel donacion)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var original = identityUserId is null
            ? null
            : await _donacionService.ObtenerParaEdicionAsync(identityUserId, donacion.NroEquipo);

        if (original is null)
        {
            return NotFound();
        }

        // no podemos editar el tipo!! se descarta lo que llegue del formulario y se muestra el guardado
        ModelState.Remove(nameof(EdicionDonacionViewModel.TipoEquipo));
        donacion.TipoEquipo = original.TipoEquipo;
        donacion.Donante = original.Donante;
        donacion.FechaRegistro = original.FechaRegistro;
        donacion.EstadoEquipo = original.EstadoEquipo;

        if (!ModelState.IsValid)
        {
            return View(donacion);
        }

        var resultado = await _donacionService.EditarAsync(identityUserId!, donacion);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(donacion);
        }

        TempData["Mensaje"] = "Los datos de la donación se actualizaron correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = donacion.NroEquipo });
    }

    [HttpPost]
    [Authorize(Policy = "UsuarioExterno")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(CancelarDonacionViewModel donacion)
    {
        var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var original = identityUserId is null
            ? null
            : await _donacionService.ObtenerDetalleAsync(identityUserId, donacion.NroEquipo);

        if (original is null)
        {
            return NotFound();
        }

        var resultado = await _donacionService.CancelarAsync(identityUserId!, donacion.NroEquipo);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(nameof(Detalle), original);
        }

        TempData["Mensaje"] = "La donación se canceló correctamente.";
        return RedirectToAction(nameof(Index));
    }
}