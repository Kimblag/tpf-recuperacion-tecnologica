using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

public class UsuarioListadoViewModel
{
    public TipoUsuario? Tipo { get; set; }

    public EstadoUsuario? Estado { get; set; }

    public IReadOnlyList<UsuarioFilaViewModel> Filas { get; set; } = [];

    public string? Orden { get; set; } = "usuario";

    public int Total { get; set; }
}
