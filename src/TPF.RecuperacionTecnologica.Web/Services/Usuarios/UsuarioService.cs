using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
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
                FechaAlta = u.FechaAlta,
                Direccion = u.Direccion,
                Telefono = u.Telefono,
                CorreoElectronico = u.CorreoElectronico
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IdentityResult> EditarAsync(string identityUserId, EdicionUsuarioViewModel modelo)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);

        if (usuario is null || usuario.EstadoUsuario != EstadoUsuario.Activo)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "EdicionFallida",
                Description = "No se pudieron guardar los cambios. Intente nuevamente."
            });
        }

        if (usuario.TipoUsuario == TipoUsuario.Persona)
        {
            var nombre = modelo.Nombre?.Trim();
            var apellido = modelo.Apellido?.Trim();

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(apellido))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DatosObligatorios",
                    Description = "El nombre y el apellido son obligatorios."
                });
            }

            usuario.Nombre = nombre;
            usuario.Apellido = apellido;
        }
        else
        {
            var razonSocial = modelo.RazonSocial?.Trim();

            if (string.IsNullOrEmpty(razonSocial))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "DatosObligatorios",
                    Description = "La razón social es obligatoria."
                });
            }

            usuario.RazonSocial = razonSocial;
        }

        usuario.Direccion = string.IsNullOrWhiteSpace(modelo.Direccion) ? null : modelo.Direccion.Trim();
        usuario.Telefono = string.IsNullOrWhiteSpace(modelo.Telefono) ? null : modelo.Telefono.Trim();
        usuario.FechaUltimaModificacion = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la edición del Usuario {NroUsuario}", usuario.NroUsuario);

            return IdentityResult.Failed(new IdentityError
            {
                Code = "EdicionFallida",
                Description = "No se pudieron guardar los cambios. Intente nuevamente."
            });
        }

        return IdentityResult.Success;
    }

    private static IQueryable<UsuarioEncontradoViewModel> ProyectarEncontrado(IQueryable<Usuario> consulta) =>
        consulta.Select(u => new UsuarioEncontradoViewModel
        {
            NroUsuario = u.NroUsuario,
            Nombre = u.RazonSocial ?? (u.Nombre + " " + u.Apellido),
            Documento = u.NroDocumento,
            CorreoElectronico = u.CorreoElectronico,
            EstadoUsuario = u.EstadoUsuario
        });

    public async Task<UsuarioEncontradoViewModel?> BuscarPorDocumentoOCorreoAsync(string texto)
    {
        var buscado = texto.Trim();

        if (buscado.Length == 0)
        {
            return null;
        }

        var consulta = _context.Usuarios.AsNoTracking();

        if (buscado.Contains('@'))
        {
            consulta = consulta.Where(u => u.CorreoElectronico == buscado);
        }
        else
        {
            var digitos = new string(buscado.Where(char.IsAsciiDigit).ToArray());

            if (digitos.Length == 0)
            {
                return null;
            }

            consulta = consulta.Where(u => u.NroDocumento == digitos);
        }

        return await ProyectarEncontrado(consulta).FirstOrDefaultAsync();
    }

    public async Task<UsuarioEncontradoViewModel?> ObtenerUsuarioEncontradoAsync(int nroUsuario) =>
        await ProyectarEncontrado(_context.Usuarios.AsNoTracking().Where(u => u.NroUsuario == nroUsuario))
            .FirstOrDefaultAsync();

    public async Task<ResultadoAltaAsistida> AltaAsistidaAsync(SelectorUsuarioViewModel datos)
    {
        var correo = datos.CorreoElectronico!.Trim();

        var existeFicha = await _context.Usuarios
            .AnyAsync(u => u.NroDocumento == datos.NroDocumento || u.CorreoElectronico == correo);
        var existeCuenta = await _userManager.FindByEmailAsync(correo) is not null;

        if (existeFicha || existeCuenta)
        {
            return ResultadoAltaAsistida.Existente();
        }

        var identityUser = new IdentityUser { UserName = correo, Email = correo };
        var resultado = await _userManager.CreateAsync(identityUser);

        if (!resultado.Succeeded)
        {
            var duplicado = resultado.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail");

            return duplicado
                ? ResultadoAltaAsistida.Existente()
                : ResultadoAltaAsistida.Fallo("No se pudo crear la cuenta de la persona.");
        }

        var esPersona = datos.TipoUsuario == TipoUsuario.Persona;

        var usuario = new Usuario
        {
            IdentityUserId = identityUser.Id,
            TipoUsuario = datos.TipoUsuario,
            Nombre = esPersona ? datos.Nombre?.Trim() : null,
            Apellido = esPersona ? datos.Apellido?.Trim() : null,
            RazonSocial = esPersona ? null : datos.RazonSocial?.Trim(),
            TipoDocumento = esPersona ? TipoDocumento.Dni : TipoDocumento.Cuit,
            NroDocumento = datos.NroDocumento!,
            Direccion = datos.Direccion?.Trim(),
            Telefono = datos.Telefono?.Trim(),
            CorreoElectronico = correo,
            EstadoUsuario = EstadoUsuario.Activo,
            FechaAlta = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return ResultadoAltaAsistida.Ok(usuario, identityUser.Id);
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
