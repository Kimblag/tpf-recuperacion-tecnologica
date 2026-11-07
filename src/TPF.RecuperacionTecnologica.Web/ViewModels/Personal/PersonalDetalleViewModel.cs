using TPF.RecuperacionTecnologica.Web.ViewModels.Auditoria;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Personal;

public class PersonalDetalleViewModel : PersonalFilaViewModel
{
    public string? Telefono { get; set; }

    public bool EsCuentaPropia { get; set; }

    public IReadOnlyList<AuditoriaFilaViewModel> Actividad { get; set; } = [];

    public int TotalActividad { get; set; }
}