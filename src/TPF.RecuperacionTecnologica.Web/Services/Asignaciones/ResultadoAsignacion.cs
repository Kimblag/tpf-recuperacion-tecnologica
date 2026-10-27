namespace TPF.RecuperacionTecnologica.Web.Services.Asignaciones;

public enum ErrorAsignacion
{
    CoordinadorNoHabilitado,
    EquipoNoDisponible,
    SolicitudNoPendiente,
    SolicitudDeOtroEquipo,
    JustificacionRequerida,
    ConfiguracionNoEncontrada,
    AsignacionConcurrente,
    Fallido
}

public record ResultadoAsignacion(int? NroAsignacion, ErrorAsignacion? Error, string? Mensaje)
{
    public bool Exitoso => NroAsignacion is not null;

    public static ResultadoAsignacion Ok(int nroAsignacion) => new(nroAsignacion, null, null);

    public static ResultadoAsignacion Fallo(ErrorAsignacion error, string mensaje) => new(null, error, mensaje);
}
