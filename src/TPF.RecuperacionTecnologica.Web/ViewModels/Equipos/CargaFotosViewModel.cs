namespace TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

public class CargaFotosViewModel
{
    public List<IFormFile> Archivos { get; set; } = [];

    public List<string?> Angulos { get; set; } = [];
}