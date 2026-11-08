namespace TPF.RecuperacionTecnologica.Web.Services.Notificaciones;

/// <summary>
/// Avisos por correo a la persona solicitante. Ningún método lanza excepciones: si el envío falla se registra
/// en el log y la operación que lo originó sigue como estaba.
/// </summary>
public interface INotificacionService
{
    /// <summary>Avisa que se asignó el equipo, con el plazo de retiro.</summary>
    Task AvisarAsignacionAsync(int nroAsignacion);

    /// <summary>Avisa que la asignación se liberó (a mano o por plazo vencido) y la solicitud se cerró, con el motivo.</summary>
    Task AvisarLiberacionAsync(int nroAsignacion);

    /// <summary>Avisa que las solicitudes se cerraron porque el equipo se entregó a otra persona.</summary>
    Task AvisarCierrePorEntregaAsync(IEnumerable<int> nroSolicitudes);

    /// <summary>Avisa que las solicitudes se cerraron porque el equipo se dio de baja.</summary>
    Task AvisarCierrePorBajaAsync(IEnumerable<int> nroSolicitudes);

    /// <summary>
    /// Envía el recordatorio de plazo próximo a vencer a las asignaciones Vigentes que entran en los días de aviso
    /// y todavía no lo recibieron. Cada asignación lo recibe una sola vez. Devuelve cuántos se enviaron.
    /// </summary>
    Task<int> EnviarAvisosDeVencimientoAsync(CancellationToken cancellationToken = default);

    /// <summary>Avisa que se entregó el equipo al donante.</summary>
    Task AvisarEntregaAlDonanteAsync(int nroEntrega);

    Task<int> AvisarIngresoSinRegistroAsync(int codigoTecnico, int? nroEquipo, string descripcion);
}