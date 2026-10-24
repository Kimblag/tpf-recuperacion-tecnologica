using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

public class SolicitudListadoViewModel
{
    private string? _busqueda;

    public string? Busqueda
    {
        get => _busqueda;
        set => _busqueda = value is { Length: > 60 } ? value[..60] : value;
    }

    public TipoSolicitante? Tipo { get; set; }

    public OrigenSolicitud? Origen { get; set; }

    public EstadoSolicitud? Estado { get; set; }

    public IReadOnlyList<SolicitudFilaViewModel> Filas { get; set; } = [];

    public int Total { get; set; }

    public const int TamanoPagina = 10;

    public int Pagina { get; set; } = 1;

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamanoPagina));

    public int Desde => Total == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;

    public int Hasta => Desde == 0 ? 0 : Desde + Filas.Count - 1;

    public bool ErrorAlCargar { get; set; }

    public bool HayFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda)
        || Tipo is not null
        || Origen is not null
        || Estado is not null;
}
