namespace TPF.RecuperacionTecnologica.Web.Services.Entregas;

public enum ErrorEntrega
{
    CoordinadorNoHabilitado,
    DatosInvalidos,
    AsignacionNoEncontrada,
    AsignacionNoVigente,
    PlazoVencido,
    DocumentoNoCoincide,
    EntregaConcurrente,
    Fallido
}

public record ResultadoEntrega(int? NroEntrega, ErrorEntrega? Error, string? Mensaje)
{
    public bool Exitoso => NroEntrega is not null;

    public static ResultadoEntrega Ok(int nroEntrega) => new(nroEntrega, null, null);

    public static ResultadoEntrega Fallo(ErrorEntrega error, string mensaje) => new(null, error, mensaje);
}
