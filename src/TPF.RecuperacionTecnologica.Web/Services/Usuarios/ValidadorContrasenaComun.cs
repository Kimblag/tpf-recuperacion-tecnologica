using Microsoft.AspNetCore.Identity;

namespace TPF.RecuperacionTecnologica.Web.Services.Usuarios;

public class ValidadorContrasenaComun : IPasswordValidator<IdentityUser>
{
    private static readonly HashSet<string> Comunes = new(StringComparer.OrdinalIgnoreCase)
    {
        "password1", "password12", "password123", "passw0rd", "contrasena1", "contrasena12", "contrasena123",
        "12345678", "123456789", "1234567890", "qwerty123", "qwertyui", "abc12345", "abcd1234",
        "iloveyou1", "welcome1", "letmein123", "changeme1", "mipassword1", "clave1234"
    };

    public Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user, string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        var parteLocal = user.Email?.Split('@')[0];

        if (Comunes.Contains(password))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "ContrasenaComun",
                Description = "Esa contraseña es muy común. Elegí otra que sea más difícil de adivinar."
            }));
        }

        if (!string.IsNullOrEmpty(parteLocal) && parteLocal.Length >= 4
            && password.Contains(parteLocal, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "ContrasenaConCorreo",
                Description = "La contraseña no puede contener tu correo electrónico."
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
