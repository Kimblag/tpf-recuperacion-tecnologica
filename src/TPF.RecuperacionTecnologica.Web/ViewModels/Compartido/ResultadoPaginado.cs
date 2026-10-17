namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

public class ResultadoPaginado<T>
{
    public IReadOnlyList<T> Elementos { get; set; } = [];

    public int Total { get; set; }

    public int Pagina { get; set; }
}
