using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.Services.Email;

namespace TPF.RecuperacionTecnologica.Web.Services.Cuenta;

public class CuentaService : ICuentaService
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IEmailSender _emailSender;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _entorno;
    private readonly ILogger<CuentaService> _logger;


    public CuentaService(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        ApplicationDbContext context,
        IAuditoriaService auditoriaService,
        IEmailSender emailSender,
        IMemoryCache cache,
        IConfiguration configuration,
        IHostEnvironment entorno,
        ILogger<CuentaService> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        _auditoriaService = auditoriaService;
        _emailSender = emailSender;
        _cache = cache;
        _configuration = configuration;
        _entorno = entorno;
        _logger = logger;
    }

    public async Task<ResultadoInicioSesion> IniciarSesionAsync(string correoElectronico, string password, bool recordarSesion)
    {
        var identityUser = await _userManager.FindByEmailAsync(correoElectronico.Trim());

        if (identityUser is null)
        {
            return ResultadoInicioSesion.CredencialesInvalidas;
        }

        // se consulta antes de verificar la contraseña para saber si este intento fue el que
        // bloqueó la cuenta o si ya estaba bloqueada de antes
        var estabaBloqueada = await _userManager.IsLockedOutAsync(identityUser);

        // verificarmos primero la pass. Así no se revela el estado de una cuenta a quien no la conoce!!
        var verificacion = await _signInManager.CheckPasswordSignInAsync(identityUser, password, lockoutOnFailure: true);

        if (verificacion.IsLockedOut)
        {
            // los intentos mientras la cuenta ya está bloqueada no se auditan, para no llenar la tabla
            if (!estabaBloqueada)
            {
                await AuditarSesionAsync(TipoOperacionAuditoria.BloqueoCuenta, identityUser.Id,
                        "Cuenta bloqueada temporalmente por intentos fallidos", actorAutenticado: false);
            }

            return ResultadoInicioSesion.CuentaBloqueada;
        }

        if (!verificacion.Succeeded)
        {
            await AuditarSesionAsync(TipoOperacionAuditoria.IntentoFallidoSesion, identityUser.Id,
                "Contraseña incorrecta", actorAutenticado: false);

            return ResultadoInicioSesion.CredencialesInvalidas;
        }

        // si la pass es correcta, verificamos que la cuenta esté habilitada
        if (!await EstaHabilitadaAsync(identityUser.Id))
        {
            return ResultadoInicioSesion.CuentaInactiva;
        }

        // Si todo está ok, generamos la cookie de sesión y guardamos los datos del usuario
        // en los claims para poder mostrarlos
        await _signInManager.SignInAsync(identityUser, recordarSesion);

        await AuditarSesionAsync(TipoOperacionAuditoria.InicioSesion, identityUser.Id, "Inicio de sesión");

        return ResultadoInicioSesion.Exitoso;
    }

    public async Task CerrarSesionAsync(string? identityUserId)
    {
        await _signInManager.SignOutAsync();

        if (!string.IsNullOrEmpty(identityUserId))
        {
            await AuditarSesionAsync(TipoOperacionAuditoria.CierreSesion, identityUserId, "Cierre de sesión");
        }
    }


    public async Task SolicitarRestablecimientoAsync(string correoElectronico, Func<string, string, string> construirRuta, string urlBaseSolicitud)
    {
        /// El correo se envía en segundo plano para evitar una mala experiencia al usuario
        var identityUser = await _userManager.FindByEmailAsync(correoElectronico.Trim());

        if (identityUser is null)
        {
            return;
        }

        // agrego esto para que cada solicitud tenga un límite de 2 minutos, para no llenar el correo 
        // si alguien hace spam solicitando muchas veces
        var claveLimite = $"recuperacion:{identityUser.Id}";

        if (_cache.TryGetValue(claveLimite, out _))
        {
            return;
        }

        // ignoramos cuentas dadas de baja 
        if (!await EstaHabilitadaAsync(identityUser.Id))
        {
            return;
        }

        // en desarrollo usamos el host actual, en prod sólo usamos el que nos pasen configurado
        // ya que pueden falsificarlo en prod
        var urlBase = _entorno.IsDevelopment()
            ? urlBaseSolicitud
            : _configuration["Aplicacion:UrlBase"];

        if (string.IsNullOrWhiteSpace(urlBase))
        {
            _logger.LogError("No se envió el enlace de recuperación: falta configurar Aplicacion:UrlBase (variable de entorno Aplicacion__UrlBase)");
            return;
        }

        _cache.Set(claveLimite, true, TimeSpan.FromMinutes(2));

        try
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);

            // como el token tiene caracteres que no viajan bien en una url se codifica en Base64Url
            var tokenCodificado = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var enlace = urlBase.TrimEnd('/') + construirRuta(identityUser.Id, tokenCodificado);

            var organizacion = await ObtenerNombreOrganizacionAsync();

            var destinatario = identityUser.Email!;
            var asunto = $"Restablecer su contraseña - {organizacion}";
            var cuerpo = PlantillaRecuperacionContrasena.Armar(enlace, organizacion);
            var idCuenta = identityUser.Id;

            // evita que se quede colgada la vista mientras se envía el mail
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendEmailAsync(destinatario, asunto, cuerpo);
                }
                catch (Exception ex)
                {
                    // si falla, liberamos para que intente de nuevo
                    _cache.Remove(claveLimite);
                    _logger.LogError(ex, "No se pudo enviar el correo de recuperación a la cuenta {IdentityUserId}", idCuenta);
                }
            });
        }
        catch (Exception ex)
        {
            // falló antes de enviar (por ejemplo al generar el token): también se libera el límite
            _cache.Remove(claveLimite);
            _logger.LogError(ex, "No se pudo preparar el correo de recuperación de la cuenta {IdentityUserId}", identityUser.Id);
        }
    }

    public async Task<IdentityResult> RestablecerContrasenaAsync(string idUsuario, string token, string nuevaContrasena)
    {
        var enlaceInvalido = IdentityResult.Failed(new IdentityError
        {
            Code = "InvalidToken",
            Description = "El enlace no es válido o ya venció. Solicite uno nuevo."
        });

        var identityUser = await _userManager.FindByIdAsync(idUsuario);

        if (identityUser is null)
        {
            return enlaceInvalido;
        }

        string tokenOriginal;

        try
        {
            tokenOriginal = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return enlaceInvalido;
        }

        var resultado = await _userManager.ResetPasswordAsync(identityUser, tokenOriginal, nuevaContrasena);

        if (!resultado.Succeeded)
        {
            // si el problema es el token, siempre se devuelve el mismo mensaje
            return resultado.Errors.Any(e => e.Code == "InvalidToken") ? enlaceInvalido : resultado;
        }

        // reestablecemos la pass y se levanta bloqueo si hay alguno
        await _userManager.SetLockoutEndDateAsync(identityUser, null);
        await _userManager.ResetAccessFailedCountAsync(identityUser);

        return resultado;
    }

    private async Task<string> ObtenerNombreOrganizacionAsync()
    {
        // el nombre sale de la configuración institucional. si todavía no se cargó, usamos uno por defecto
        var nombre = await _context.ConfiguracionInstitucional
            .AsNoTracking()
            .Select(c => c.NombreOrganizacion)
            .FirstOrDefaultAsync();

        return string.IsNullOrWhiteSpace(nombre) ? "Recuperación Tecnológica" : nombre.Trim();
    }

    private async Task AuditarSesionAsync(TipoOperacionAuditoria tipoOperacion, string identityUserId, string detalle, bool actorAutenticado = true)
    {
        // la auditoría nunca debe impedir iniciar o cerrar sesión: si falla, solo registramos en el log
        try
        {
            var ficha = await ObtenerFichaAuditoriaAsync(identityUserId);

            if (ficha is null)
            {
                return;
            }

            await _auditoriaService.RegistrarEventoAsync(
                tipoOperacion,
                actorAutenticado ? ficha.Value.CodigoPersonal : null,
                ficha.Value.Entidad,
                ficha.Value.Id,
                detalle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar el evento {TipoOperacion} de la cuenta {IdentityUserId}",
                tipoOperacion, identityUserId);
        }
    }

    private async Task<(int? CodigoPersonal, string Entidad, int Id)?> ObtenerFichaAuditoriaAsync(string identityUserId)
    {
        // el personal se audita como actor, es decir con CodigoPersonal, pero
        // el usuario externo no es personal, va en null
        var codigoPersonal = await _context.PersonalInterno
            .Where(p => p.IdentityUserId == identityUserId)
            .Select(p => (int?)p.CodigoPersonal)
            .SingleOrDefaultAsync();

        if (codigoPersonal is not null)
        {
            return (codigoPersonal, nameof(Personal), codigoPersonal.Value);
        }

        var nroUsuario = await _context.Usuarios
            .Where(u => u.IdentityUserId == identityUserId)
            .Select(u => (int?)u.NroUsuario)
            .SingleOrDefaultAsync();

        if (nroUsuario is null)
        {
            return null;
        }

        return (null, nameof(Usuario), nroUsuario.Value);
    }


    // TODO: cuando exista la baja de personal, hay que invalidar las sesiones (la baja de Usuario ya lo hace)
    // abiertas con UserManager.UpdateSecurityStampAsync, porque esta verificación solo corre al iniciar sesión
    private async Task<bool> EstaHabilitadaAsync(string identityUserId)
    {
        var estadoPersonal = await _context.PersonalInterno
            .Where(p => p.IdentityUserId == identityUserId)
            .Select(p => (EstadoPersonal?)p.EstadoPersonal)
            .SingleOrDefaultAsync();

        if (estadoPersonal is not null)
        {
            return estadoPersonal == EstadoPersonal.Activo;
        }

        var estadoUsuario = await _context.Usuarios
            .Where(u => u.IdentityUserId == identityUserId)
            .Select(u => (EstadoUsuario?)u.EstadoUsuario)
            .SingleOrDefaultAsync();

        if (estadoUsuario is not null)
        {
            return estadoUsuario == EstadoUsuario.Activo;
        }

        _logger.LogWarning("La cuenta {IdentityUserId} no tiene ficha de Usuario ni de Personal", identityUserId);

        return false;
    }
}