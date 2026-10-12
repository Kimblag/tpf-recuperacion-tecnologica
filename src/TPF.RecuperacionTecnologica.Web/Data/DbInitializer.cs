using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.Data;

public static class DbInitializer
{
    // se cargan datos no sensibles para el primer Administrador
    // el mail y la pass salen de user secrets
    private const string DniInicial = "00000000";
    private const string NombreInicial = "Administrador";
    private const string ApellidoInicial = "Inicial";

    // crea los roles de Identity, uno por cada RolPersonal; el nombre del rol es el nombre del enum
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));

        try
        {
            foreach (var rol in Enum.GetNames<RolPersonal>())
            {
                if (await roleManager.RoleExistsAsync(rol))
                {
                    continue;
                }

                var resultado = await roleManager.CreateAsync(new IdentityRole(rol));

                if (!resultado.Succeeded)
                {
                    logger.LogError("No se pudo crear el rol {Rol}: {Errores}",
                        rol, string.Join(", ", resultado.Errors.Select(e => e.Description)));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudieron crear los roles. ¿Está la base migrada? (dotnet ef database update)");
        }
    }

    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));

        try
        {
            // creamos el admin solo si todavía no existe ninguno
            var administrador = await context.PersonalInterno
                .FirstOrDefaultAsync(p => p.RolAsignado == RolPersonal.Administrador);

            if (administrador is null)
            {
                var correo = configuration["SeedAdmin:Correo"]?.Trim();
                var password = configuration["SeedAdmin:Password"];

                // sin credenciales no se agrega nada ni hay valores por defecto
                if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password))
                {
                    logger.LogWarning(
                        "No hay ningún Administrador y no se ha agregado uno: faltan SeedAdmin:Correo y SeedAdmin:Password en user-secrets.");
                    return;
                }

                administrador = await CrearAdministradorAsync(context, userManager, correo, password);
                logger.LogInformation("Se creó el primer Administrador ({Correo}).", correo);
            }

            if (!await context.ConfiguracionInstitucional.AnyAsync())
            {
                context.ConfiguracionInstitucional.Add(new ConfiguracionInstitucional
                {
                    Id = 1,
                    NombreOrganizacion = "Organización de prueba",
                    ZonaCobertura = "Zona de prueba",
                    DiasLimiteRetiro = 7,
                    CodigoAdministrador = administrador.CodigoPersonal
                });

                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo crear el Administrador. ¿Está la base migrada? (dotnet ef database update)");
        }
    }

    private static async Task<Personal> CrearAdministradorAsync(
        ApplicationDbContext context, UserManager<IdentityUser> userManager, string correo, string password)
    {
        // la cuenta y la ficha se guardan juntas
        await using var transaccion = await context.Database.BeginTransactionAsync();

        var user = new IdentityUser { UserName = correo, Email = correo };
        var resultado = await userManager.CreateAsync(user, password);

        if (!resultado.Succeeded)
        {
            var errores = string.Join(", ", resultado.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"No se pudo crear el Administrador: {errores}");
        }

        // el rol se asigna dentro de la transacción: si falla, no queda un Administrador sin rol
        var rolAsignado = await userManager.AddToRoleAsync(user, nameof(RolPersonal.Administrador));

        if (!rolAsignado.Succeeded)
        {
            var errores = string.Join(", ", rolAsignado.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"No se pudo asignar el rol al Administrador: {errores}");
        }

        var administrador = new Personal
        {
            IdentityUserId = user.Id,
            Dni = DniInicial,
            Nombre = NombreInicial,
            Apellido = ApellidoInicial,
            CorreoElectronico = correo,
            RolAsignado = RolPersonal.Administrador,
            EstadoPersonal = EstadoPersonal.Activo
        };

        context.PersonalInterno.Add(administrador);
        await context.SaveChangesAsync();
        await transaccion.CommitAsync();

        return administrador;
    }
}