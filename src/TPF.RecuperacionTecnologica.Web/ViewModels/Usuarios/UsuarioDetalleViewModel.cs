namespace TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

public class UsuarioDetalleViewModel : UsuarioFilaViewModel
{
    public string? Direccion { get; set; }

    public DateTime? FechaBaja { get; set; }

    public string? MotivoBaja { get; set; }
}