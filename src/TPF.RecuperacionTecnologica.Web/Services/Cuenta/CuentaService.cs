using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.Services.Cuenta;

public class CuentaService : ICuentaService
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CuentaService> _logger;

    public CuentaService(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        ApplicationDbContext context,
        ILogger<CuentaService> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public async Task<ResultadoInicioSesion> IniciarSesionAsync(string correoElectronico, string password, bool recordarSesion)
    {
        var identityUser = await _userManager.FindByEmailAsync(correoElectronico.Trim());

        if (identityUser is null)
        {
            return ResultadoInicioSesion.CredencialesInvalidas;
        }
        
        // verificarmos primero la pass. Así no se revela el estado de una cuenta a quien no la conoce!!
        var verificacion = await _signInManager.CheckPasswordSignInAsync(identityUser, password, lockoutOnFailure: true);

        if (verificacion.IsLockedOut)
        {
            return ResultadoInicioSesion.CuentaBloqueada;
        }

        if (!verificacion.Succeeded)
        {
            return ResultadoInicioSesion.CredencialesInvalidas;
        }

        // si la pass es correcta, verificanis que la cuenta esté habilitada
        if (!await EstaHabilitadaAsync(identityUser.Id))
        {
            return ResultadoInicioSesion.CuentaInactiva;
        }

        // Si todo está ok, generamos la cookie de sesión
        await _signInManager.SignInAsync(identityUser, recordarSesion);

        return ResultadoInicioSesion.Exitoso;
    }

    public Task CerrarSesionAsync() => _signInManager.SignOutAsync();

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
