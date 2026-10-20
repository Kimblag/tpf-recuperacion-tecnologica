namespace TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;

public class EquipoCatalogoDetalleViewModel : EquipoDisponibleViewModel
{
    public DateTime? FechaDisponibilidad { get; set; }

    public IReadOnlyList<(string UrlImagen, string? Angulo)> Imagenes { get; set; } = [];
}
