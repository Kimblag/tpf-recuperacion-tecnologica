using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

public class RegistroDonacionAsistidaViewModel : RegistroDonacionViewModel
{
    public SelectorUsuarioViewModel Usuario { get; set; } = new();

    public string Coordinador { get; set; } = string.Empty;
}
