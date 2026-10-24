using TPF.RecuperacionTecnologica.Web.Models;

namespace TPF.RecuperacionTecnologica.Web.Services.Usuarios;

public record ResultadoAltaAsistida(Usuario? Usuario, string? IdentityUserId, bool YaExiste, string? Mensaje)
{
    public const string MensajeYaExiste = "Ya existe una cuenta con ese DNI o CUIT, o con ese correo. Buscarla con la opción \"Usuario existente\".";

    public bool Exitoso => Usuario is not null;

    public static ResultadoAltaAsistida Ok(Usuario usuario, string identityUserId) => new(usuario, identityUserId, false, null);

    public static ResultadoAltaAsistida Existente() => new(null, null, true, MensajeYaExiste);

    public static ResultadoAltaAsistida Fallo(string mensaje) => new(null, null, false, mensaje);
}
