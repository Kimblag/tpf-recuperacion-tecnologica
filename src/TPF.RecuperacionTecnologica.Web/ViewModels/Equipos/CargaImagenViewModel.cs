using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

public class CargaImagenViewModel
{
    [Required(ErrorMessage = "El número de equipo es obligatorio.")]
    public int? NroEquipo { get; set; }

    [Required(ErrorMessage = "Seleccioná la imagen a cargar.")]
    [Display(Name = "Imagen")]
    public IFormFile? Archivo { get; set; }

    [StringLength(40, ErrorMessage = "El ángulo no puede superar los 40 caracteres.")]
    [Display(Name = "Ángulo")]
    public string? Angulo { get; set; }

    // datos de contexto para mostrar el formulario; no viajan en el envío
    public string CodigoEquipo { get; set; } = string.Empty;

    public string MarcaYModelo { get; set; } = string.Empty;
}
