using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.Models.Enums;

public enum TipoEquipo
{
    Notebook,

    [Display(Name = "PC de escritorio")]
    PcEscritorio,

    Monitor,

    Celular,

    [Display(Name = "Periférico")]
    Periferico
}
