using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Cuenta;
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

    private const string MensajeConcurrencia = "Los datos cambiaron mientras los editabas. Volvé a abrir la ficha e intentá nuevamente.";

    private static IdentityResult Error(string codigo, string descripcion) =>
        IdentityResult.Failed(new IdentityError { Code = codigo, Description = descripcion });

    private static readonly IReadOnlyDictionary<string, Expression<Func<PersonalEntidad, object>>> ColumnasOrden =
        new Dictionary<string, Expression<Func<PersonalEntidad, object>>>
        {
            ["codigo"] = p => p.CodigoPersonal,
            ["nombre"] = p => p.Apellido + " " + p.Nombre,
            ["dni"] = p => p.Dni,
            ["correo"] = p => p.CorreoElectronico,
            ["rol"] = p => p.RolAsignado,
            ["estado"] = p => p.EstadoPersonal
        };

    public const string AccesoBloqueadas = "bloqueadas";

    public async Task<PersonalListadoViewModel> ListarAsync(string? busqueda, RolPersonal? rol, EstadoPersonal? estado, string? acceso, string? orden, int pagina, bool filtroInvalido = false)
    {
        var consulta = _context.PersonalInterno.AsNoTracking();
        var ahora = DateTimeOffset.UtcNow;

        if (rol.HasValue && !Enum.IsDefined(rol.Value))
        {
            rol = null;
            filtroInvalido = true;
        }

        if (estado.HasValue && !Enum.IsDefined(estado.Value))
        {
            estado = null;
            filtroInvalido = true;
        }

        if (!string.IsNullOrWhiteSpace(acceso) && acceso != AccesoBloqueadas)
        {
            acceso = null;
            filtroInvalido = true;
        }

        if (!string.IsNullOrWhiteSpace(orden) && !ColumnasOrden.ContainsKey(OrdenListado.Interpretar(orden).Columna))
        {
            orden = null;
            filtroInvalido = true;
        }

        if (acceso == AccesoBloqueadas)
        {
            consulta = consulta.Where(p => _context.Users.Any(i => i.Id == p.IdentityUserId && i.LockoutEnd > ahora));
        }

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
            var dni = termino.Replace(".", "");
            var buscaDni = dni.Length > 0 && dni.All(char.IsAsciiDigit);

            var textoCodigo = termino.StartsWith("PER-", StringComparison.OrdinalIgnoreCase) ? termino[4..] : termino;
            int? codigo = textoCodigo.All(char.IsAsciiDigit) && int.TryParse(textoCodigo, out var numero) ? numero : null;

            var patron = BusquedaTexto.Patron(termino);

            consulta = consulta.Where(p =>
                EF.Functions.Like(EF.Functions.Collate(p.Nombre + " " + p.Apellido, BusquedaTexto.Colacion), patron)
                || EF.Functions.Like(EF.Functions.Collate(p.Apellido + " " + p.Nombre, BusquedaTexto.Colacion), patron)
                || (buscaDni && p.Dni.Contains(dni))
                || (codigo != null && p.CodigoPersonal == codigo));
        }

        var hayPersonal = await _context.PersonalInterno.AnyAsync();
        var total = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)PersonalListadoViewModel.TamanoPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var filas = await consulta
            .Ordenar(orden, ColumnasOrden, p => p.CodigoPersonal, c => c.OrderBy(p => p.CodigoPersonal))
            .Skip((pagina - 1) * PersonalListadoViewModel.TamanoPagina)
            .Take(PersonalListadoViewModel.TamanoPagina)
            .Select(p => new PersonalFilaViewModel
            {
                CodigoPersonal = p.CodigoPersonal,
                Dni = p.Dni,
                Nombre = p.Nombre,
                Apellido = p.Apellido,
                CorreoElectronico = p.CorreoElectronico,
                RolAsignado = p.RolAsignado,
                EstadoPersonal = p.EstadoPersonal,
                EsRaiz = p.EsRaiz,
                EstaBloqueada = _context.Users.Any(i => i.Id == p.IdentityUserId && i.LockoutEnd > ahora)
            })
            .ToListAsync();

        return new PersonalListadoViewModel
        {
            Busqueda = busqueda,
            Rol = rol,
            Estado = estado,
            Acceso = acceso,
            FiltroInvalido = filtroInvalido,
            HayPersonal = hayPersonal,
            Orden = orden ?? "codigo",
            Filas = filas,
            Total = total,
            Pagina = pagina
        };
    }

    public async Task<IdentityResult> DesbloquearAsync(int codigoPersonal, int codigoAdministrador)
    {
        var identityUserId = await _context.PersonalInterno
            .Where(p => p.CodigoPersonal == codigoPersonal)
            .Select(p => p.IdentityUserId)
            .FirstOrDefaultAsync();

        var identityUser = identityUserId is null ? null : await _userManager.FindByIdAsync(identityUserId);

        if (identityUser is null)
        {
            return Error("PersonalNoEncontrado", "No se encontró el personal especificado.");
        }

        if (codigoPersonal == codigoAdministrador)
        {
            return Error("CuentaPropia", "No se puede desbloquear la propia cuenta. Otro Administrador debe hacerlo.");
        }

        if (!await _userManager.IsLockedOutAsync(identityUser))
        {
            return Error("NoBloqueada", "La cuenta no está bloqueada.");
        }

        await _userManager.SetLockoutEndDateAsync(identityUser, null);
        await _userManager.ResetAccessFailedCountAsync(identityUser);

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.DesbloqueoCuenta, codigoAdministrador, nameof(Models.Personal), codigoPersonal,
                "Desbloqueo de cuenta realizado por un Administrador");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar el desbloqueo del Personal {CodigoPersonal}", codigoPersonal);
        }

        return IdentityResult.Success;
    }

    public async Task<int?> ObtenerCodigoPersonalAsync(string identityUserId)
    {
        return await _context.PersonalInterno
            .Where(p => p.IdentityUserId == identityUserId)
            .Select(p => (int?)p.CodigoPersonal)
            .FirstOrDefaultAsync();
    }

    public async Task<MiCuentaViewModel?> ObtenerMiCuentaAsync(string identityUserId)
    {
        return await _context.PersonalInterno
            .AsNoTracking()
            .Where(p => p.IdentityUserId == identityUserId)
            .Select(p => new MiCuentaViewModel
            {
                CodigoPersonal = p.CodigoPersonal,
                Nombre = p.Nombre,
                Apellido = p.Apellido,
                CorreoElectronico = p.CorreoElectronico,
                RolAsignado = p.RolAsignado,
                EstadoPersonal = p.EstadoPersonal
            })
            .FirstOrDefaultAsync();
    }

    public async Task<PersonalDetalleViewModel?> ObtenerDetalleAsync(int codigoPersonal)
    {
        var ahora = DateTimeOffset.UtcNow;

        return await _context.PersonalInterno
            .AsNoTracking()
            .Where(p => p.CodigoPersonal == codigoPersonal)
            .Select(p => new PersonalDetalleViewModel
            {
                CodigoPersonal = p.CodigoPersonal,
                Dni = p.Dni,
                Nombre = p.Nombre,
                Apellido = p.Apellido,
                Telefono = p.Telefono,
                CorreoElectronico = p.CorreoElectronico,
                RolAsignado = p.RolAsignado,
                EstadoPersonal = p.EstadoPersonal,
                EsRaiz = p.EsRaiz,
                EstaBloqueada = _context.Users.Any(i => i.Id == p.IdentityUserId && i.LockoutEnd > ahora),
                RowVersion = p.RowVersion
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IdentityResult> EditarAsync(
        EdicionPersonalViewModel modelo,
        int codigoAdministrador)
    {
        if (modelo.RolAsignado is null || !Enum.IsDefined(modelo.RolAsignado.Value))
        {
            return Error("RolInvalido", "El rol asignado no es válido.");
        }

        var rolNuevo = modelo.RolAsignado.Value;

        var personal = await _context.PersonalInterno
            .FirstOrDefaultAsync(p => p.CodigoPersonal == modelo.CodigoPersonal);

        if (personal is null)
        {
            return Error("PersonalNoEncontrado",
                "No se encontró el personal especificado.");
        }

        if (personal.CodigoPersonal == codigoAdministrador)
        {
            return Error("CuentaPropia",
                "No se puede editar la propia cuenta. Otro Administrador debe hacerlo.");
        }

        if (personal.EsRaiz)
        {
            return Error("AdministradorPrincipal",
                "No se puede editar al Administrador principal.");
        }

        if (modelo.RowVersion.Length > 0 && !personal.RowVersion.SequenceEqual(modelo.RowVersion))
        {
            return Error("DatosModificados", MensajeConcurrencia);
        }

        var correo = modelo.CorreoElectronico.Trim().ToLowerInvariant();
        var dni = modelo.Dni.Trim();

        if (await _context.PersonalInterno.AnyAsync(p =>
            p.CodigoPersonal != modelo.CodigoPersonal && p.Dni == dni))
        {
            return Error("DniDuplicado",
                "Ya existe un integrante del Personal con ese DNI.");
        }

        if (await _context.PersonalInterno.AnyAsync(p =>
            p.CodigoPersonal != modelo.CodigoPersonal &&
            p.CorreoElectronico == correo))
        {
            return Error("CorreoDuplicado",
                "Ya existe un integrante del Personal con ese correo electrónico.");
        }

        var cuentaConEseCorreo = await _userManager.FindByEmailAsync(correo);

        if (cuentaConEseCorreo is not null && cuentaConEseCorreo.Id != personal.IdentityUserId)
        {
            return Error("CorreoEnUso",
                "Ese correo ya está en uso por otra cuenta del sistema.");
        }

        var rolAnterior = personal.RolAsignado;
        var dniAnterior = personal.Dni;

        if (rolAnterior == RolPersonal.Administrador
            && rolNuevo != RolPersonal.Administrador
            && personal.EstadoPersonal == EstadoPersonal.Activo
            && !await _context.PersonalInterno.AnyAsync(p =>
                p.CodigoPersonal != personal.CodigoPersonal
                && p.RolAsignado == RolPersonal.Administrador
                && p.EstadoPersonal == EstadoPersonal.Activo))
        {
            return Error("UltimoAdministrador",
                "No se puede cambiar el rol del último Administrador activo.");
        }

        var identityUser = await _userManager.FindByIdAsync(personal.IdentityUserId);

        if (identityUser is null)
        {
            return Error("CuentaNoEncontrada",
                "No se encontró la cuenta de acceso asociada al personal.");
        }

        var nuevoRol = rolNuevo.ToString();

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            if (!string.Equals(identityUser.Email, correo,
                StringComparison.OrdinalIgnoreCase))
            {
                var resultadoCorreo =
                    await _userManager.SetEmailAsync(identityUser, correo);

                if (!resultadoCorreo.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return resultadoCorreo;
                }
            }

            if (!string.Equals(identityUser.UserName, correo,
                StringComparison.OrdinalIgnoreCase))
            {
                var resultadoUsuario =
                    await _userManager.SetUserNameAsync(identityUser, correo);

                if (!resultadoUsuario.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return resultadoUsuario;
                }
            }

            var rolesActuales = await _userManager.GetRolesAsync(identityUser);

            var rolesARemover = rolesActuales
                .Where(r => !string.Equals(r, nuevoRol, StringComparison.Ordinal))
                .ToArray();

            if (rolesARemover.Length > 0)
            {
                var resultadoRemover =
                    await _userManager.RemoveFromRolesAsync(identityUser, rolesARemover);

                if (!resultadoRemover.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return resultadoRemover;
                }
            }

            if (!rolesActuales.Contains(nuevoRol))
            {
                var resultadoAgregar =
                    await _userManager.AddToRoleAsync(identityUser, nuevoRol);

                if (!resultadoAgregar.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return resultadoAgregar;
                }
            }

            personal.Dni = dni;
            personal.Nombre = modelo.Nombre.Trim();
            personal.Apellido = modelo.Apellido.Trim();
            personal.Telefono = string.IsNullOrWhiteSpace(modelo.Telefono)
                ? null
                : modelo.Telefono.Trim();
            personal.CorreoElectronico = correo;
            personal.RolAsignado = rolNuevo;
            personal.FechaUltimaModificacion = DateTime.UtcNow;

            if (modelo.RowVersion.Length > 0)
            {
                _context.Entry(personal).Property(p => p.RowVersion).OriginalValue = modelo.RowVersion;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Error("DatosModificados", MensajeConcurrencia);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex,
                "Error al editar el personal con código {CodigoPersonal}",
                modelo.CodigoPersonal);

            return Error("EdicionFallida",
                "No se pudo completar la edición. Intentá nuevamente.");
        }

        if (rolAnterior != personal.RolAsignado)
        {
            await _userManager.UpdateSecurityStampAsync(identityUser);
        }

        var cambios = new List<string>();

        if (rolAnterior != personal.RolAsignado)
        {
            cambios.Add($"cambio de rol: {NombreEnum.De(rolAnterior)} -> {NombreEnum.De(personal.RolAsignado)}");
        }

        if (dniAnterior != personal.Dni)
        {
            cambios.Add($"cambio de DNI: {dniAnterior} -> {personal.Dni}");
        }

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.EdicionPersonal,
                codigoAdministrador,
                nameof(Models.Personal),
                personal.CodigoPersonal,
                cambios.Count == 0
                    ? $"Edición de personal ({NombreEnum.De(personal.RolAsignado)})"
                    : $"Edición de personal ({string.Join("; ", cambios)})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "No se pudo auditar la edición del Personal {CodigoPersonal}",
                personal.CodigoPersonal);
        }

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> InactivarAsync(
        InactivarPersonalViewModel modelo,
        int codigoAdministrador)
    {
        var personal = await _context.PersonalInterno
            .FirstOrDefaultAsync(p => p.CodigoPersonal == modelo.CodigoPersonal);
        if (personal is null)
        {
            return Error("PersonalNoEncontrado",
                "No se encontró el personal especificado.");
        }
        if (personal.EstadoPersonal == EstadoPersonal.Inactivo)
        {
            return Error("YaInactivo",
                "El personal ya se encuentra inactivo.");
        }
        if (personal.EsRaiz)
        {
            return Error("AdministradorPrincipal",
                "No se puede inactivar al Administrador principal.");
        }
        if (personal.CodigoPersonal == codigoAdministrador)
        {
            return Error("CuentaPropia",
                "No se puede inactivar la propia cuenta. Otro Administrador debe hacerlo.");
        }
        // no se puede dejar al sistema sin ningún Administrador activo
        if (personal.RolAsignado == RolPersonal.Administrador
            && !await _context.PersonalInterno.AnyAsync(p =>
                p.CodigoPersonal != personal.CodigoPersonal
                && p.RolAsignado == RolPersonal.Administrador
                && p.EstadoPersonal == EstadoPersonal.Activo))
        {
            return Error("UltimoAdministrador",
                "No se puede inactivar al último Administrador activo.");
        }
        var identityUser = await _userManager.FindByIdAsync(personal.IdentityUserId);

        if (identityUser is null)
        {
            return Error("CuentaNoEncontrada",
                "No se encontró la cuenta de acceso asociada al personal.");
        }
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            personal.EstadoPersonal = EstadoPersonal.Inactivo;
            personal.FechaInactivacion = DateTime.UtcNow;
            personal.MotivoInactivacion = modelo.MotivoInactivacion?.Trim();
            personal.FechaUltimaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex,
                "Error al inactivar el personal con código {CodigoPersonal}",
                modelo.CodigoPersonal);
            return Error("InactivacionFallida",
                "No se pudo completar la inactivación. Intentá nuevamente.");
        }
        // Invalidar las sesiones existentes de Identity

        try
        {
            await _userManager.UpdateSecurityStampAsync(identityUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudieron invalidar las sesiones del personal {CodigoPersonal}",
                personal.CodigoPersonal);
        }
        // Regristar la operación en la auditoría

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
             TipoOperacionAuditoria.InactivacionPersonal,
             codigoAdministrador,
             nameof(Models.Personal),
             personal.CodigoPersonal,
             $"Inactivación de personal ({personal.RolAsignado}). Motivo: {personal.MotivoInactivacion}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "No se pudo auditar la inactivación del Personal {CodigoPersonal}",
                personal.CodigoPersonal);
        }
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> AltaAsync(AltaPersonalViewModel modelo, int codigoAdministrador)
    {
        if (modelo.RolAsignado is null || !Enum.IsDefined(modelo.RolAsignado.Value))
        {
            return Error("RolInvalido", "El rol asignado no es válido.");
        }

        var rol = modelo.RolAsignado.Value;
        var dni = modelo.Dni.Trim();
        var correo = modelo.CorreoElectronico.Trim().ToLowerInvariant();

        var errores = new List<IdentityError>();

        if (await _context.PersonalInterno.AnyAsync(p => p.Dni == dni))
        {
            errores.Add(new IdentityError { Code = "DniDuplicado", Description = "Ya existe un integrante del Personal con ese DNI." });
        }

        if (await _context.PersonalInterno.AnyAsync(p => p.CorreoElectronico == correo))
        {
            errores.Add(new IdentityError { Code = "CorreoDuplicado", Description = "Ya existe un integrante del Personal con ese correo electrónico." });
        }
        else if (await _userManager.FindByEmailAsync(correo) is not null)
        {
            errores.Add(new IdentityError { Code = "CorreoEnUso", Description = "Ese correo ya está en uso por otra cuenta del sistema." });
        }

        if (errores.Count > 0)
        {
            return IdentityResult.Failed(errores.ToArray());
        }

        var identityUser = new IdentityUser { UserName = correo, Email = correo };
        var resultado = await _userManager.CreateAsync(identityUser, modelo.Password);

        if (!resultado.Succeeded)
        {
            return resultado;
        }

        // 3. rol de Identity igual al rol asignado; si falla se elimina la cuenta
        var nombreRol = rol.ToString();

        try
        {
            var rolAsignado = await _userManager.AddToRoleAsync(identityUser, nombreRol);

            if (!rolAsignado.Succeeded)
            {
                _logger.LogError("No se pudo asignar el rol {Rol} a la cuenta del Personal: {Errores}",
                    nombreRol, string.Join(", ", rolAsignado.Errors.Select(e => e.Description)));
                await EliminarCuentaAsync(identityUser);
                return Error("RolNoAsignado", "No se pudo asignar el rol a la cuenta. Intentá nuevamente.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la asignación del rol {Rol}; se elimina la cuenta creada", nombreRol);
            await EliminarCuentaAsync(identityUser);
            return Error("RolNoAsignado", "No se pudo asignar el rol a la cuenta. Intentá nuevamente.");
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
            RolAsignado = rol,
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
            return Error("AltaFallida", "No se pudo completar el alta. Intentá nuevamente.");
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

    public async Task<IdentityResult> ReactivarAsync(int codigoPersonal, int codigoAdministrador)
    {
        var personal = await _context.PersonalInterno
            .FirstOrDefaultAsync(p => p.CodigoPersonal == codigoPersonal);

        if (personal is null)
        {
            return Error("PersonalNoEncontrado", "No se encontró el personal especificado.");
        }

        if (personal.EstadoPersonal == EstadoPersonal.Activo)
        {
            return Error("YaActivo", "El personal ya se encuentra activo.");
        }

        try
        {
            personal.EstadoPersonal = EstadoPersonal.Activo;
            personal.FechaInactivacion = null;
            personal.MotivoInactivacion = null;
            personal.FechaUltimaModificacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar el personal con código {CodigoPersonal}", codigoPersonal);
            return Error("ReactivacionFallida", "No se pudo completar la reactivación. Intentá nuevamente.");
        }

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.ReactivacionPersonal,
                codigoAdministrador,
                nameof(Models.Personal),
                personal.CodigoPersonal,
                $"Reactivación de personal ({personal.RolAsignado})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar la reactivación del Personal {CodigoPersonal}", personal.CodigoPersonal);
        }

        return IdentityResult.Success;
    }
}