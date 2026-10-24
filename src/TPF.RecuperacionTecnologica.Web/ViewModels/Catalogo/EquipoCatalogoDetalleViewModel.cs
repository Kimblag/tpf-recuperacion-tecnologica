using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;

public class EquipoCatalogoDetalleViewModel : EquipoDisponibleViewModel
{
    public DictamenTecnico DictamenTecnico { get; set; }

    public DateTime FechaRevision { get; set; }

    public IReadOnlyList<(string UrlImagen, string? Angulo)> Imagenes { get; set; } = [];
}