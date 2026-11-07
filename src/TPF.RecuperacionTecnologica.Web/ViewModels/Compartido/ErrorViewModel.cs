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
        404 => "La dirección no existe o la página ya no está disponible. Revisá el enlace o volvé al inicio.",
        403 => "Tu cuenta no tiene permiso para ver esta página.",
        429 => "Se hicieron demasiados pedidos en poco tiempo. Esperá un minuto e intentá de nuevo.",
        _ => "No se pudo completar la operación. Intentá de nuevo en unos minutos. Si el problema sigue, informalo a soporte con el código de referencia."
    };
}
