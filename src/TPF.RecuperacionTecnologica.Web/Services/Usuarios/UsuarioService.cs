using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.NativeInterop;
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

        Usuario usuario;

        // 2 la cuenta y ficha se guardan juntas, es todo o nada para evitar inconsistencias
        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            var identityUser = new IdentityUser { UserName = correo, Email = correo };
            var resultado = await _userManager.CreateAsync(identityUser, modelo.Password);

            if (!resultado.Succeeded)
            {
                // al tener el user name igual al correo, va a generar error por duplicados (email, username)
                // unifico los mensajes en uno
                var correoDuplicado = resultado.Errors.Any(e => e.Code == "DuplicateUserName" || e.Code == "DuplicateEmail");

                // sale sin commitear la transaccion, no se guarda nada
                return correoDuplicado ? IdentityResult.Failed(
                    new IdentityError
                {
                    Code = "CorreoDuplicado",
                    Description = "Ya existe una cuenta con ese correo electrónico."
                }) : resultado;
            }

            var esPersona = modelo.TipoUsuario == TipoUsuario.Persona;
            usuario = new Usuario
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

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el alta del Usuario; se revirtió la transacción");

            return IdentityResult.Failed(new IdentityError
            {
                Code = "AltaFallida",
                Description = "No se pudo completar el registro. Intente nuevamente."
            });
        }


        // registrar auditoria fuera de la transaccion porque si falla, el registro ya estara hecho
        // no se debe informar error
        try
        {
            await _auditoriaService.RegistrarEventoAsync(
            TipoOperacionAuditoria.AltaUsuario, null, nameof(Usuario), usuario.NroUsuario,
            $"Alta de usuario ({usuario.TipoUsuario})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar el alta del Usuario {NroUsuario}", usuario.NroUsuario);
        }
        return IdentityResult.Success;
    }
}
