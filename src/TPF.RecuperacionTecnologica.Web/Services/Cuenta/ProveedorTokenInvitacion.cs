using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace TPF.RecuperacionTecnologica.Web.Services.Cuenta;

/// <summary>
/// Genera y verifica los tokens del enlace de invitación. Tiene un vencimiento propio, más largo que el
/// del restablecimiento de contraseña, porque una persona invitada suele abrir el correo mucho después.
/// </summary>
public class ProveedorTokenInvitacion : DataProtectorTokenProvider<IdentityUser>
{
    public const string Nombre = "Invitacion";
    public const string Proposito = "Activacion";

    public ProveedorTokenInvitacion(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<ProveedorTokenInvitacionOptions> options,
        ILogger<DataProtectorTokenProvider<IdentityUser>> logger)
        : base(dataProtectionProvider, options, logger)
    {
    }
}

public class ProveedorTokenInvitacionOptions : DataProtectionTokenProviderOptions
{
    public ProveedorTokenInvitacionOptions()
    {
        Name = ProveedorTokenInvitacion.Nombre;
        TokenLifespan = TimeSpan.FromDays(3);
    }
}
