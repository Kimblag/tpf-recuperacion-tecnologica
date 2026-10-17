using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

public class MisDonacionesViewModel
{
    public string? Busqueda { get; set; }

    public EstadoEquipo? Estado { get; set; }

    public IReadOnlyList<DonacionFilaViewModel> Filas { get; set; } = [];

    public int Total { get; set; }
}