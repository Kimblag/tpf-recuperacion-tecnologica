using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

public class UsuarioListadoViewModel
{
    public const int TamanoPagina = 10;

    public string? Busqueda { get; set; }

    public TipoUsuario? Tipo { get; set; }

    public EstadoUsuario? Estado { get; set; }

    public string? Acceso { get; set; }

    public bool FiltroInvalido { get; set; }

    public IReadOnlyList<UsuarioFilaViewModel> Filas { get; set; } = [];

    public string? Orden { get; set; } = "usuario";

    public int Total { get; set; }

    public int Pagina { get; set; } = 1;

    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamanoPagina));

    public int Desde => Total == 0 ? 0 : (Pagina - 1) * TamanoPagina + 1;

    public int Hasta => Desde == 0 ? 0 : Desde + Filas.Count - 1;
}
