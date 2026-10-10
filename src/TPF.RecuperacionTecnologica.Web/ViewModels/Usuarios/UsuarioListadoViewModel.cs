using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

public class UsuarioListadoViewModel
{
    public string? Busqueda { get; set; }

    public EstadoUsuario? Estado { get; set; }

    public IReadOnlyList<UsuarioFilaViewModel> Filas { get; set; } = [];

    public int Total { get; set; }
}
