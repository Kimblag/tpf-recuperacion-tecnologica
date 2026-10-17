using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

public class MisDonacionesViewModel
{
    public string? Busqueda { get; set; }

    public EstadoEquipo? Estado { get; set; }

    public TipoEquipo? Tipo { get; set; }

    public EstadoInicialDeclarado? Condicion { get; set; }

    public IReadOnlyList<DonacionFilaViewModel> Filas { get; set; } = [];

    public int Total { get; set; }

    public bool HayFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda)
        || Estado is not null
        || Tipo is not null
        || Condicion is not null;
}