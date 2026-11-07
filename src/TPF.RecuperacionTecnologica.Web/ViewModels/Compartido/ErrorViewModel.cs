namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    /// <summary>
    /// Código HTTP del error. Si no viene, se trata como un error inesperado (500).
    /// </summary>
    public int? Codigo { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public string Titulo => Codigo switch
    {
        404 => "No se encontró la página",
        403 => "No hay acceso a esta sección",
        429 => "Demasiados intentos seguidos",
        _ => "Ocurrió un error inesperado"
    };

    public string Mensaje => Codigo switch
    {
        404 => "La dirección no existe o la página ya no está disponible. Revisar el enlace o volver al inicio.",
        403 => "La cuenta con la que se inició sesión no tiene permiso para ver esta página.",
        429 => "Se hicieron demasiados pedidos en poco tiempo. Esperar un minuto e intentar de nuevo.",
        _ => "No se pudo completar la operación. Intentar de nuevo en unos minutos. Si el problema sigue, informarlo a soporte con el código de referencia."
    };
}
