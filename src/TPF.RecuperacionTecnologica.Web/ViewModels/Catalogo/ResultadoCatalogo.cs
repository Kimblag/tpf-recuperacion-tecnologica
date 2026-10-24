namespace TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;

public class ResultadoCatalogo
{
    public IReadOnlyList<EquipoDisponibleViewModel> Elementos { get; set; } = [];
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TotalDisponibles { get; set; }
    public IReadOnlyList<string> Marcas { get; set; } = [];

}
