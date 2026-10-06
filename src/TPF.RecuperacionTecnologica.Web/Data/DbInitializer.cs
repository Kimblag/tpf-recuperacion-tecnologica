using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.Data;

public static class DbInitializer
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // crea el Administrador solo si todavía no existe ninguno
        var administrador = await context.PersonalInterno
            .FirstOrDefaultAsync(p => p.RolAsignado == RolPersonal.Administrador);

        if (administrador is null)
        {
            var user = new IdentityUser
            {
                UserName = "admin@admin.com",
                Email = "admin@admin.com"
            };

            var resultado = await userManager.CreateAsync(user, "Admin1234");

            if (!resultado.Succeeded)
            {
                var errores = string.Join(", ", resultado.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"No se pudo crear el Administrador: {errores}");
            }

            administrador = new Personal
            {
                IdentityUserId = user.Id,
                Dni = "123445",
                Nombre = "Admin",
                Apellido = "Admin",
                CorreoElectronico = "admin@admin.com",
                RolAsignado = RolPersonal.Administrador,
                EstadoPersonal = EstadoPersonal.Activo
            };

            context.PersonalInterno.Add(administrador);
            await context.SaveChangesAsync();
        }

        // crea la fila única de configuración solo si todavía no existe
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
}
