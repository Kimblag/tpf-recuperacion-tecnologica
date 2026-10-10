using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Personal;

public class PersonalListadoViewModel
{
    public string? Busqueda { get; set; }

    public RolPersonal? Rol { get; set; }

    public EstadoPersonal? Estado { get; set; }

    public IReadOnlyList<PersonalFilaViewModel> Filas { get; set; } = [];

    public int Total { get; set; }
}
