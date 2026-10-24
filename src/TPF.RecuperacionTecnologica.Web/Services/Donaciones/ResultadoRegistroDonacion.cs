namespace TPF.RecuperacionTecnologica.Web.Services.Donaciones;

public enum ErrorRegistroDonacion
{
    CoordinadorNoHabilitado,
    UsuarioNoHabilitado,
    UsuarioYaExiste,
    Fallido
}

public record ResultadoRegistroDonacion(int? NroEquipo, ErrorRegistroDonacion? Error, string? Mensaje)
{
    public bool Exitoso => NroEquipo is not null;

    public static ResultadoRegistroDonacion Ok(int nroEquipo) => new(nroEquipo, null, null);

    public static ResultadoRegistroDonacion Fallo(ErrorRegistroDonacion error, string mensaje) => new(null, error, mensaje);
}
