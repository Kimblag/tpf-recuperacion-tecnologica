using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;

namespace TPF.RecuperacionTecnologica.Web.Services.Usuarios;

public class UsuarioService : IUsuarioService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<UsuarioService> _logger;


    public UsuarioService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IAuditoriaService auditoriaService,
        ILogger<UsuarioService> logger)
    {
        _context = context;
        _userManager = userManager;
        _auditoriaService = auditoriaService;
        _logger = logger;
    }

    public async Task<IdentityResult> RegistrarAsync(RegistroUsuarioViewModel modelo)
    {
        var correo = modelo.CorreoElectronico.Trim();

        // 1. validar que el documento sea único
        var documentoExiste = await _context.Usuarios.AnyAsync(u => u.NroDocumento == modelo.NroDocumento);

        if (documentoExiste)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "DocumentoDuplicado",
                Description = "Ya existe un usuario registrado con ese número de documento."
            });
        }

        // crear la cuenta de usuario
        var identityUser = new IdentityUser { UserName = correo, Email = correo };
        var resultado = await _userManager.CreateAsync(identityUser, modelo.Password);

        if (!resultado.Succeeded)
        {
            // como el UserName es el correo, un correo repetido devuelve dos errores en inglés; se unifican en uno
            var correoDuplicado = resultado.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName");

            return correoDuplicado
                ? IdentityResult.Failed(new IdentityError
                {
                    Code = "CorreoDuplicado",
                    Description = "Ya existe una cuenta con ese correo electrónico."
                })
                : resultado;
        }

        // crear la ficha del usuario
        var esPersona = modelo.TipoUsuario == TipoUsuario.Persona;
        var usuario = new Usuario
        {
            IdentityUserId = identityUser.Id,
            TipoUsuario = modelo.TipoUsuario,
            Nombre = esPersona ? modelo.Nombre?.Trim() : null,
            Apellido = esPersona ? modelo.Apellido?.Trim() : null,
            RazonSocial = esPersona ? null : modelo.RazonSocial?.Trim(),
            TipoDocumento = modelo.TipoDocumento,
            NroDocumento = modelo.NroDocumento,
            Direccion = modelo.Direccion?.Trim(),
            Telefono = modelo.Telefono?.Trim(),
            CorreoElectronico = correo,
            EstadoUsuario = EstadoUsuario.Activo,
            FechaAlta = DateTime.UtcNow
        };


        try
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Si falla la ficha, entonces se borra la cuenta recién creada por consistencia de datos
            _context.Entry(usuario).State = EntityState.Detached;
            var borrado = await _userManager.DeleteAsync(identityUser);

            if (!borrado.Succeeded)
            {
                _logger.LogCritical("No se pudo eliminar la cuenta huérfana {IdentityUserId}", identityUser.Id);
            }

            _logger.LogError(ex, "Falló el alta del Usuario; se eliminó la cuenta {IdentityUserId}", identityUser.Id);

            return IdentityResult.Failed(new IdentityError
            {
                Code = "AltaFallida",
                Description = "No se pudo completar el registro. Intente nuevamente."
            });
        }


        // se audita si todo sale ok
        await _auditoriaService.RegistrarEventoAsync(TipoOperacionAuditoria.AltaUsuario, null, nameof(Usuario), usuario.NroUsuario, $"Alta de usuario ({usuario.TipoUsuario})");

        return IdentityResult.Success;
    }
}
