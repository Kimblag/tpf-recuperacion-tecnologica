namespace TPF.RecuperacionTecnologica.Web.Services.Solicitudes;

public enum ErrorRegistroSolicitud
{
    UsuarioNoHabilitado,
    TipoSolicitanteInvalido,
    EquipoNoDisponible,
    EquipoPropio,
    SolicitudDuplicada,
    LimiteAlcanzado,
    Fallido
}

public record ResultadoRegistroSolicitud(int? NroSolicitud, ErrorRegistroSolicitud? Error, string? Mensaje)
{
    public bool Exitoso => NroSolicitud is not null;

    public static ResultadoRegistroSolicitud Ok(int nroSolicitud) => new(nroSolicitud, null, null);

    public static ResultadoRegistroSolicitud Fallo(ErrorRegistroSolicitud error, string mensaje) => new(null, error, mensaje);
}


public record ImpedimentoSolicitud(ErrorRegistroSolicitud Error, string Mensaje);