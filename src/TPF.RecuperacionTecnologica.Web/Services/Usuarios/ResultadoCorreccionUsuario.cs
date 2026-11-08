namespace TPF.RecuperacionTecnologica.Web.Services.Usuarios;

public record ResultadoCorreccionUsuario(bool Exitoso, string? Mensaje, bool CorreoCambio, string? IdentityUserId)
{
    public static ResultadoCorreccionUsuario Ok(bool correoCambio, string identityUserId) => new(true, null, correoCambio, identityUserId);

    public static ResultadoCorreccionUsuario Fallo(string mensaje) => new(false, mensaje, false, null);
}
