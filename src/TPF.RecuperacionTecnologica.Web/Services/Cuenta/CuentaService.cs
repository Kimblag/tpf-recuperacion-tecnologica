using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;

namespace TPF.RecuperacionTecnologica.Web.Services.Cuenta;

public class CuentaService : ICuentaService
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<CuentaService> _logger;

    public CuentaService(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        ApplicationDbContext context,
        IAuditoriaService auditoriaService,
        ILogger<CuentaService> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        _auditoriaService = auditoriaService;
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
        var claims = new List<Claim>();
        var identidad = await ObtenerIdentidadVisibleAsync(identityUser.Id);

        if (identidad is not null && !string.IsNullOrWhiteSpace(identidad.Value.Nombre))
        {
            claims.Add(new Claim("nombre", identidad.Value.Nombre));

            if (!string.IsNullOrWhiteSpace(identidad.Value.Iniciales))
            {
                claims.Add(new Claim("iniciales", identidad.Value.Iniciales));
            }

            // etiqueta para mostrar en pantalla, no se usa para autorizar
            if (!string.IsNullOrWhiteSpace(identidad.Value.Rol))
            {
                claims.Add(new Claim("rol", identidad.Value.Rol));
            }
        }

        await _signInManager.SignInWithClaimsAsync(identityUser, recordarSesion, claims);

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

    private async Task<(string Nombre, string Iniciales, string? Rol)?> ObtenerIdentidadVisibleAsync(string identityUserId)
    {
        var personal = await _context.PersonalInterno
            .Where(p => p.IdentityUserId == identityUserId)
            .Select(p => new { p.Nombre, p.Apellido, p.RolAsignado })
            .SingleOrDefaultAsync();

        if (personal is not null)
        {
            return ($"{personal.Nombre} {personal.Apellido}".Trim(),
                InicialesPersona(personal.Nombre, personal.Apellido),
                EtiquetaRol(personal.RolAsignado));
        }

        var usuario = await _context.Usuarios
            .Where(u => u.IdentityUserId == identityUserId)
            .Select(u => new { u.TipoUsuario, u.Nombre, u.Apellido, u.RazonSocial })
            .SingleOrDefaultAsync();

        if (usuario is null)
        {
            return null;
        }

        // los usuarios externos no muestran etiqueta de rol
        if (usuario.TipoUsuario == TipoUsuario.Persona)
        {
            return ($"{usuario.Nombre} {usuario.Apellido}".Trim(), InicialesPersona(usuario.Nombre, usuario.Apellido), null);
        }

        // institución muestra una sola letra, la primera de la razón social, tal cual
        var razonSocial = usuario.RazonSocial?.Trim() ?? string.Empty;

        return (razonSocial, PrimeraLetra(razonSocial), null);
    }

    private static string EtiquetaRol(RolPersonal rol) => rol switch
    {
        RolPersonal.Tecnico => "Técnico",
        RolPersonal.Coordinador => "Coordinador",
        RolPersonal.Administrador => "Administrador",
        _ => rol.ToString()
    };

    private static string InicialesPersona(string? nombre, string? apellido) =>
        PrimeraLetra(nombre) + PrimeraLetra(apellido);

    private static string PrimeraLetra(string? texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? string.Empty
            : StringInfo.GetNextTextElement(texto.Trim()).ToUpperInvariant();

    // TODO: cuando exista la baja de usuarios y de personal, hay que invalidar las sesiones
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