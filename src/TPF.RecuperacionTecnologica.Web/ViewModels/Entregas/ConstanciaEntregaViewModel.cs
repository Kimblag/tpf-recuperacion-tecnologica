namespace TPF.RecuperacionTecnologica.Web.ViewModels.Entregas;

public class ConstanciaEntregaViewModel
{
    public string NombreOrganizacion { get; set; } = string.Empty;

    public EntregaDetalleViewModel Entrega { get; set; } = null!;
}
