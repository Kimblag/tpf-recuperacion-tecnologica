using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;

public enum OrdenCatalogo
{
    [Display(Name = "Más recientes primero")]
    Recientes,

    [Display(Name = "Más antiguos primero")]
    Antiguos,

    [Display(Name = "Marca (A-Z)")]
    MarcaAsc,

    [Display(Name = "Marca (Z-A)")]
    MarcaDesc
}
