using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;
using TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

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

    // por seguridad correo y documento duplicados devuelven el mismo resultado porque así no se confirma cuál de los dos datos ya existe
    private static IdentityResult RegistroDuplicado() =>
        IdentityResult.Failed(new IdentityError
        {
            Code = "RegistroDuplicado",
            Description = "No se pudo completar el registro con los datos ingresados. Si ya tiene una cuenta, inicie sesión o recupere su contraseña."
        });

    public async Task<IdentityResult> RegistrarAsync(RegistroUsuarioViewModel modelo)
    {
        var correo = modelo.CorreoElectronico.Trim();

        // 1. validar que el documento sea único
        var documentoExiste = await _context.Usuarios.AnyAsync(u => u.NroDocumento == modelo.NroDocumento);

        if (documentoExiste)
        {
            return RegistroDuplicado();
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
                return correoDuplicado ? RegistroDuplicado() : resultado;
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

    public async Task<UsuarioListadoViewModel> ListarAsync(TipoUsuario? tipo, EstadoUsuario? estado)
    {
        var consulta = _context.Usuarios.AsNoTracking();

        if (tipo.HasValue)
        {
            consulta = consulta.Where(u => u.TipoUsuario == tipo.Value);
        }

        if (estado.HasValue)
        {
            consulta = consulta.Where(u => u.EstadoUsuario == estado.Value);
        }

        var filas = await consulta
            .OrderBy(u => u.NroUsuario)
            .Select(u => new UsuarioFilaViewModel
            {
                NroUsuario = u.NroUsuario,
                TipoUsuario = u.TipoUsuario,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                RazonSocial = u.RazonSocial,
                TipoDocumento = u.TipoDocumento,
                NroDocumento = u.NroDocumento,
                CorreoElectronico = u.CorreoElectronico,
                Telefono = u.Telefono,
                FechaAlta = u.FechaAlta,
                EstadoUsuario = u.EstadoUsuario
            })
            .ToListAsync();

        return new UsuarioListadoViewModel
        {
            Tipo = tipo,
            Estado = estado,
            Filas = filas,
            Total = filas.Count
        };
    }

    public async Task<UsuarioDetalleViewModel?> ObtenerDetalleAsync(int nroUsuario)
    {
        return await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.NroUsuario == nroUsuario)
            .Select(u => new UsuarioDetalleViewModel
            {
                NroUsuario = u.NroUsuario,
                TipoUsuario = u.TipoUsuario,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                RazonSocial = u.RazonSocial,
                TipoDocumento = u.TipoDocumento,
                NroDocumento = u.NroDocumento,
                CorreoElectronico = u.CorreoElectronico,
                Telefono = u.Telefono,
                Direccion = u.Direccion,
                FechaAlta = u.FechaAlta,
                EstadoUsuario = u.EstadoUsuario
            })
            .FirstOrDefaultAsync();
    }

    public async Task<EdicionUsuarioViewModel?> ObtenerEdicionAsync(string identityUserId)
    {
        return await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.IdentityUserId == identityUserId)
            .Select(u => new EdicionUsuarioViewModel
            {
                TipoUsuario = u.TipoUsuario,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                RazonSocial = u.RazonSocial,
                TipoDocumento = u.TipoDocumento,
                NroDocumento = u.NroDocumento,
                Direccion = u.Direccion,
                Telefono = u.Telefono,
                CorreoElectronico = u.CorreoElectronico
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(IdentityResult Resultado, bool CorreoCambiado)> EditarAsync(string identityUserId, EdicionUsuarioViewModel modelo)
    {
        var edicionFallida = IdentityResult.Failed(new IdentityError
        {
            Code = "EdicionFallida",
            Description = "No se pudieron guardar los cambios. Intente nuevamente."
        });

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);

        // un Usuario inactivo no llega hasta acá porque el login lo bloquea, pero se valida igual
        if (usuario is null || usuario.EstadoUsuario != EstadoUsuario.Activo)
        {
            return (edicionFallida, false);
        }

        var correo = modelo.CorreoElectronico.Trim();
        var correoCambiado = correo != usuario.CorreoElectronico;

        // la cuenta y la ficha se guardan juntas, es todo o nada para que el correo no quede distinto en cada una
        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            if (correoCambiado)
            {
                var identityUser = await _userManager.FindByIdAsync(identityUserId);

                if (identityUser is null)
                {
                    return (edicionFallida, false);
                }

                // el user name es el correo, así que se actualizan los dos
                var resultado = await _userManager.SetEmailAsync(identityUser, correo);

                if (resultado.Succeeded)
                {
                    resultado = await _userManager.SetUserNameAsync(identityUser, correo);
                }

                if (!resultado.Succeeded)
                {
                    // igual que en el registro, no se confirma que el correo pertenece a otra cuenta
                    var correoDuplicado = resultado.Errors.Any(e => e.Code == "DuplicateUserName" || e.Code == "DuplicateEmail");

                    var error = correoDuplicado
                        ? IdentityResult.Failed(new IdentityError
                        {
                            Code = "CorreoNoDisponible",
                            Description = "No se pudo usar ese correo electrónico. Ingrese otro."
                        })
                        : resultado;

                    // sale sin commitear la transaccion, no se guarda nada
                    return (error, false);
                }
            }

            usuario.Direccion = string.IsNullOrWhiteSpace(modelo.Direccion) ? null : modelo.Direccion.Trim();
            usuario.Telefono = string.IsNullOrWhiteSpace(modelo.Telefono) ? null : modelo.Telefono.Trim();
            usuario.CorreoElectronico = correo;
            usuario.FechaUltimaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la edición del Usuario {NroUsuario}; se revirtió la transacción", usuario.NroUsuario);

            return (edicionFallida, false);
        }

        return (IdentityResult.Success, correoCambiado);
    }

    public async Task<IdentityResult> DarDeBajaAsync(string identityUserId, BajaUsuarioViewModel modelo)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);

        if (usuario is null || usuario.EstadoUsuario != EstadoUsuario.Activo)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "BajaFallida",
                Description = "No se pudo dar de baja la cuenta."
            });
        }

        // la verificación y la baja van en la misma transacción para que no cambie el estado de las operaciones entre medio
        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            var errores = new List<IdentityError>();

            // PendienteRecepcion no bloquea: la organización todavía no recibió el equipo
            var tieneEquiposComprometidos = await _context.Equipos.AnyAsync(e =>
                e.NroUsuarioDonante == usuario.NroUsuario
                && (e.EstadoEquipo == EstadoEquipo.Disponible
                    || e.EstadoEquipo == EstadoEquipo.EnReparacion
                    || e.EstadoEquipo == EstadoEquipo.Asignado));

            if (tieneEquiposComprometidos)
            {
                errores.Add(new IdentityError
                {
                    Code = "EquiposComprometidos",
                    Description = "No puede darse de baja porque tiene equipos donados que la organización ya recibió o tiene comprometidos."
                });
            }

            var tieneSolicitudesAbiertas = await _context.Solicitudes.AnyAsync(s =>
                s.NroUsuario == usuario.NroUsuario
                && (s.EstadoSolicitud == EstadoSolicitud.Pendiente
                    || s.EstadoSolicitud == EstadoSolicitud.Asignada));

            if (tieneSolicitudesAbiertas)
            {
                errores.Add(new IdentityError
                {
                    Code = "SolicitudesAbiertas",
                    Description = "No puede darse de baja porque tiene solicitudes pendientes o asignadas."
                });
            }

            if (errores.Count > 0)
            {
                return IdentityResult.Failed(errores.ToArray());
            }

            usuario.EstadoUsuario = EstadoUsuario.Inactivo;
            usuario.FechaBaja = DateTime.UtcNow;
            usuario.MotivoBaja = modelo.MotivoBaja.Trim();
            usuario.FechaUltimaModificacion = usuario.FechaBaja;

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la baja del Usuario {NroUsuario}; se revirtió la transacción", usuario.NroUsuario);

            return IdentityResult.Failed(new IdentityError
            {
                Code = "BajaFallida",
                Description = "No se pudo dar de baja la cuenta. Intente nuevamente."
            });
        }

        // la baja ya está guardada: lo que falle de acá en adelante se registra pero no la deshace
        try
        {
            var identityUser = await _userManager.FindByIdAsync(identityUserId);

            if (identityUser is not null)
            {
                // invalida las sesiones abiertas (otros dispositivos) sin esperar a que venzan
                await _userManager.UpdateSecurityStampAsync(identityUser);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudieron invalidar las sesiones del Usuario {NroUsuario} tras la baja", usuario.NroUsuario);
        }

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.BajaUsuario, null, nameof(Usuario), usuario.NroUsuario,
                $"Baja de usuario ({usuario.TipoUsuario})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar la baja del Usuario {NroUsuario}", usuario.NroUsuario);
        }

        return IdentityResult.Success;
    }
}
