using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Personal;
using PersonalEntidad = TPF.RecuperacionTecnologica.Web.Models.Personal;

namespace TPF.RecuperacionTecnologica.Web.Services.Personal;

public class PersonalService : IPersonalService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<PersonalService> _logger;

    public PersonalService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IAuditoriaService auditoriaService,
        ILogger<PersonalService> logger)
    {
        _context = context;
        _userManager = userManager;
        _auditoriaService = auditoriaService;
        _logger = logger;
    }

    private static IdentityResult Error(string codigo, string descripcion) =>
        IdentityResult.Failed(new IdentityError { Code = codigo, Description = descripcion });

    public async Task<PersonalListadoViewModel> ListarAsync(string? busqueda, RolPersonal? rol, EstadoPersonal? estado)
    {
        var consulta = _context.PersonalInterno.AsNoTracking();

        if (rol.HasValue)
        {
            consulta = consulta.Where(p => p.RolAsignado == rol.Value);
        }

        if (estado.HasValue)
        {
            consulta = consulta.Where(p => p.EstadoPersonal == estado.Value);
        }

        var termino = busqueda?.Trim();

        if (!string.IsNullOrEmpty(termino))
        {
            // el DNI se muestra con puntos (27.884.310), así que se acepta escrito así
            var dni = termino.Replace(".", "");
            var buscaDni = dni.Length > 0;

            // el código se muestra como PER-0002, y también se acepta solo el número
            var textoCodigo = termino.StartsWith("PER-", StringComparison.OrdinalIgnoreCase) ? termino[4..] : termino;
            int? codigo = int.TryParse(textoCodigo, out var numero) ? numero : null;

            consulta = consulta.Where(p =>
                (p.Nombre + " " + p.Apellido).Contains(termino)
                || (buscaDni && p.Dni.Contains(dni))
                || (codigo != null && p.CodigoPersonal == codigo));
        }

        var filas = await consulta
            .OrderBy(p => p.CodigoPersonal)
            .Select(p => new PersonalFilaViewModel
            {
                CodigoPersonal = p.CodigoPersonal,
                Dni = p.Dni,
                Nombre = p.Nombre,
                Apellido = p.Apellido,
                CorreoElectronico = p.CorreoElectronico,
                RolAsignado = p.RolAsignado,
                EstadoPersonal = p.EstadoPersonal
            })
            .ToListAsync();

        return new PersonalListadoViewModel
        {
            Busqueda = busqueda,
            Rol = rol,
            Estado = estado,
            Filas = filas,
            Total = filas.Count
        };
    }

    public async Task<int?> ObtenerCodigoPersonalAsync(string identityUserId)
    {
        return await _context.PersonalInterno
            .Where(p => p.IdentityUserId == identityUserId)
            .Select(p => (int?)p.CodigoPersonal)
            .FirstOrDefaultAsync();
    }

    public async Task<IdentityResult> AltaAsync(AltaPersonalViewModel modelo, int codigoAdministrador)
    {
        var dni = modelo.Dni.Trim();
        var correo = modelo.CorreoElectronico.Trim();

        // 1. DNI y correo únicos en Personal; quien llama es Administrador, así que se informa cuál está repetido
        var errores = new List<IdentityError>();

        if (await _context.PersonalInterno.AnyAsync(p => p.Dni == dni))
        {
            errores.Add(new IdentityError { Code = "DniDuplicado", Description = "Ya existe un integrante del Personal con ese DNI." });
        }

        if (await _context.PersonalInterno.AnyAsync(p => p.CorreoElectronico == correo))
        {
            errores.Add(new IdentityError { Code = "CorreoDuplicado", Description = "Ya existe un integrante del Personal con ese correo electrónico." });
        }

        if (errores.Count > 0)
        {
            return IdentityResult.Failed(errores.ToArray());
        }

        // 2. cuenta de Identity (si el correo ya lo usa la cuenta de un Usuario, Identity lo informa)
        var identityUser = new IdentityUser { UserName = correo, Email = correo };
        var resultado = await _userManager.CreateAsync(identityUser, modelo.Password);

        if (!resultado.Succeeded)
        {
            return resultado;
        }

        // 3. rol de Identity igual al rol asignado; si falla se elimina la cuenta
        var nombreRol = modelo.RolAsignado.ToString();

        try
        {
            var rolAsignado = await _userManager.AddToRoleAsync(identityUser, nombreRol);

            if (!rolAsignado.Succeeded)
            {
                _logger.LogError("No se pudo asignar el rol {Rol} a la cuenta del Personal: {Errores}",
                    nombreRol, string.Join(", ", rolAsignado.Errors.Select(e => e.Description)));
                await EliminarCuentaAsync(identityUser);
                return Error("RolNoAsignado", "No se pudo asignar el rol a la cuenta. Intente nuevamente.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la asignación del rol {Rol}; se elimina la cuenta creada", nombreRol);
            await EliminarCuentaAsync(identityUser);
            return Error("RolNoAsignado", "No se pudo asignar el rol a la cuenta. Intente nuevamente.");
        }

        // 4. registro de Personal; si falla se elimina la cuenta
        var personal = new PersonalEntidad
        {
            IdentityUserId = identityUser.Id,
            Dni = dni,
            Nombre = modelo.Nombre.Trim(),
            Apellido = modelo.Apellido.Trim(),
            Telefono = string.IsNullOrWhiteSpace(modelo.Telefono) ? null : modelo.Telefono.Trim(),
            CorreoElectronico = correo,
            RolAsignado = modelo.RolAsignado,
            EstadoPersonal = EstadoPersonal.Activo,
            FechaAlta = DateTime.UtcNow
        };

        try
        {
            _context.PersonalInterno.Add(personal);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el alta del Personal; se elimina la cuenta de Identity creada");
            _context.Entry(personal).State = EntityState.Detached;
            await EliminarCuentaAsync(identityUser);
            return Error("AltaFallida", "No se pudo completar el alta. Intente nuevamente.");
        }

        // 5. auditoría una vez hecha el alta: si falla, no se informa error
        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.AltaPersonal, codigoAdministrador, nameof(Models.Personal), personal.CodigoPersonal,
                $"Alta de personal ({personal.RolAsignado})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar el alta del Personal {CodigoPersonal}", personal.CodigoPersonal);
        }

        return IdentityResult.Success;
    }

    private async Task EliminarCuentaAsync(IdentityUser identityUser)
    {
        try
        {
            var resultado = await _userManager.DeleteAsync(identityUser);

            if (!resultado.Succeeded)
            {
                _logger.LogError("No se pudo eliminar la cuenta {IdentityUserId} tras un alta fallida: {Errores}",
                    identityUser.Id, string.Join(", ", resultado.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo eliminar la cuenta {IdentityUserId} tras un alta fallida", identityUser.Id);
        }
    }
}
