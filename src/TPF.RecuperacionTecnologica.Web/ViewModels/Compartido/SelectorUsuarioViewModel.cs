using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

public enum ModoUsuarioAsistido
{
    Existente,
    Nuevo
}

public class UsuarioEncontradoViewModel
{
    public int NroUsuario { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Documento { get; set; } = string.Empty;

    public string CorreoElectronico { get; set; } = string.Empty;

    public EstadoUsuario EstadoUsuario { get; set; }
}

public class SelectorUsuarioViewModel
{
    public ModoUsuarioAsistido Modo { get; set; }

    [Display(Name = "DNI o correo electrónico")]
    [StringLength(254, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Busqueda { get; set; }

    public int? NroUsuario { get; set; }

    public bool BusquedaRealizada { get; set; }

    public UsuarioEncontradoViewModel? Encontrado { get; set; }

    [Display(Name = "Tipo de usuario")]
    public TipoUsuario TipoUsuario { get; set; }

    [StringLength(100, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Nombre { get; set; }

    [StringLength(100, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Apellido { get; set; }

    [Display(Name = "Razón social")]
    [StringLength(150, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? RazonSocial { get; set; }

    [Display(Name = "Número de documento")]
    [StringLength(11, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? NroDocumento { get; set; }

    [Display(Name = "Dirección")]
    [StringLength(200, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Direccion { get; set; }

    [Display(Name = "Teléfono")]
    [StringLength(20, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Telefono { get; set; }

    [Display(Name = "Correo electrónico")]
    [StringLength(254, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? CorreoElectronico { get; set; }
}
