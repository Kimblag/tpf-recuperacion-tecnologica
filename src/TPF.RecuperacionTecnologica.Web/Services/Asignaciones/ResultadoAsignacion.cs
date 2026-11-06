namespace TPF.RecuperacionTecnologica.Web.Services.Asignaciones;

public enum ErrorAsignacion
{
    CoordinadorNoHabilitado,
    EquipoNoDisponible,
    SolicitudNoPendiente,
    SolicitudDeOtroEquipo,
    JustificacionRequerida,
    ConfiguracionNoEncontrada,
    AsignacionNoEncontrada,
    AsignacionNoVigente,
    AsignacionNoVencida,
    MotivoRequerido,
    AsignacionConcurrente,
    Fallido
}

public record ResultadoAsignacion(int? NroAsignacion, ErrorAsignacion? Error, string? Mensaje)
{
    public bool Exitoso => NroAsignacion is not null;

    public static ResultadoAsignacion Ok(int nroAsignacion) => new(nroAsignacion, null, null);

    public static ResultadoAsignacion Fallo(ErrorAsignacion error, string mensaje) => new(null, error, mensaje);
}


/// <summary>
/// Resumen de una corrida de liberación automática: cuáles se liberaron, cuántas se omitieron
/// porque cambiaron en ese momento (por ejemplo, se entregaron) y cuántas fallaron.
/// </summary>
public record ResultadoLiberacionAutomatica(IReadOnlyList<int> Liberadas, int Omitidas, int Fallidas);
