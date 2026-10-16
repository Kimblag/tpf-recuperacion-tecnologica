using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

public class EdicionUsuarioViewModel
{
    // estos datos se muestran pero no se editan: el servicio nunca los lee del formulario
    public TipoUsuario TipoUsuario { get; set; }

    public string? Nombre { get; set; }

    public string? Apellido { get; set; }

    [Display(Name = "Razón social")]
    public string? RazonSocial { get; set; }

    public TipoDocumento TipoDocumento { get; set; }

    [Display(Name = "Número de documento")]
    public string NroDocumento { get; set; } = string.Empty;

    [Display(Name = "Dirección")]
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres.")]
    public string? Direccion { get; set; }

    [Display(Name = "Teléfono")]
    [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
    public string? Telefono { get; set; }

    [Display(Name = "Correo electrónico")]
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(254, ErrorMessage = "Máximo 254 caracteres.")]
    public string CorreoElectronico { get; set; } = string.Empty;
}
