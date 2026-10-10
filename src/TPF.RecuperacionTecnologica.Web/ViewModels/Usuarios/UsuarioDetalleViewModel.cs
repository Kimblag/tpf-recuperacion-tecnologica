namespace TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

public class UsuarioDetalleViewModel : UsuarioFilaViewModel
{
    public string? Direccion { get; set; }

    public int DonacionesRegistradas { get; set; }

    public int SolicitudesRealizadas { get; set; }

    public int EntregasRecibidas { get; set; }
}
