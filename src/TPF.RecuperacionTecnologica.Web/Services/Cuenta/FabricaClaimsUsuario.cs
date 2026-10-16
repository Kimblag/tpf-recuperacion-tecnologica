using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.NativeInterop;
using System.Globalization;
using System.Security.Claims;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.Services.Cuenta;

public class FabricaClaimsUsuario : UserClaimsPrincipalFactory<IdentityUser, IdentityRole>
{
    private readonly ApplicationDbContext _context;

    public FabricaClaimsUsuario(UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> opciones,
        ApplicationDbContext context) 
        : base(userManager, roleManager, opciones)
    {
        _context = context;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(IdentityUser user)
    {
        var identidad = await base.GenerateClaimsAsync(user);
        var datos = await ObtenerDatosVisiblesAsync(user.Id);

        if (datos is null || string.IsNullOrWhiteSpace(datos.Value.Nombre))
        {
            return identidad;
        }

        identidad.AddClaim(new Claim(ClaimsSesion.Nombre, datos.Value.Nombre));

        if (!string.IsNullOrWhiteSpace(datos.Value.Iniciales))
        {
            identidad.AddClaim(new Claim(ClaimsSesion.Iniciales, datos.Value.Iniciales));
        }

        if (!string.IsNullOrWhiteSpace(datos.Value.Rol))
        {
            identidad.AddClaim(new Claim(ClaimsSesion.Rol, datos.Value.Rol));
        }

        if (datos.Value.TipoUsuario is not null)
        {
            identidad.AddClaim(new Claim(ClaimsSesion.TipoUsuario, datos.Value.TipoUsuario.Value.ToString()));
        }

        return identidad;
    }


    private async Task<(string Nombre, string Iniciales, string? Rol, TipoUsuario? TipoUsuario)?> ObtenerDatosVisiblesAsync(string identityUserId)
    {
        var personal = await _context.PersonalInterno
            .Where(p => p.IdentityUserId == identityUserId)
            .Select(p => new { p.Nombre, p.Apellido, p.RolAsignado })
            .SingleOrDefaultAsync();

        if (personal is not null)
        {
            return ($"{personal.Nombre} {personal.Apellido}".Trim(),
                InicialesPersona(personal.Nombre, personal.Apellido),
                EtiquetaRol(personal.RolAsignado),
                null);
        }

        var usuario = await _context.Usuarios
            .Where(u => u.IdentityUserId == identityUserId)
            .Select(u => new { u.TipoUsuario, u.Nombre, u.Apellido, u.RazonSocial })
            .SingleOrDefaultAsync();


        if (usuario is null)
        {
            return null;
        }

        if (usuario.TipoUsuario == TipoUsuario.Persona)
        {
            return ($"{usuario.Nombre} {usuario.Apellido}".Trim(),
                InicialesPersona(usuario.Nombre, usuario.Apellido), null, usuario.TipoUsuario);
        }

         var razonSocial = usuario.RazonSocial?.Trim() ?? string.Empty;

        return (razonSocial, PrimeraLetra(razonSocial), null, usuario.TipoUsuario);
    }

    private static string EtiquetaRol(RolPersonal rol) => rol switch
    {
        RolPersonal.Tecnico => "Técnico",
        RolPersonal.Coordinador => "Coordinador",
        RolPersonal.Administrador => "Administrador",
        _ => rol.ToString()
    };

    private static string InicialesPersona(string? nombre, string? apellido) =>
        PrimeraLetra(nombre) + PrimeraLetra(apellido);

    private static string PrimeraLetra(string? texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? string.Empty
            : StringInfo.GetNextTextElement(texto.Trim()).ToUpperInvariant();
}
