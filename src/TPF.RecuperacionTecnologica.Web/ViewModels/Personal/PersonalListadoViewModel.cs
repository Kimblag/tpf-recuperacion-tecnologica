using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Personal;

public class PersonalListadoViewModel
{
    public const int TamanoPagina = 10;

    public string? Busqueda { get; set; }

    public RolPersonal? Rol { get; set; }

    public EstadoPersonal? Estado { get; set; }

    public string? Acceso { get; set; }

    public bool FiltroInvalido { get; set; }

    public bool HayPersonal { get; set; }

    public int? CodigoActual { get; set; }

    public IReadOnlyList<PersonalFilaViewModel> Filas { get; set; } = [];

    public string? Orden { get; set; } = "codigo";

    public int Total { get; set; }

    public int Pagina { get; set; } = 1;

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamanoPagina));

    public int Desde => Total == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;

    public int Hasta => Desde == 0 ? 0 : Desde + Filas.Count - 1;
}
