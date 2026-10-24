using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

public class EquipoSeleccionableViewModel
{
    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string MarcaYModelo { get; set; } = string.Empty;

    public string CodigoEquipo => NroEquipo.ToString("D6");
}

public class SelectorEquipoViewModel
{
    [Display(Name = "Número, marca o modelo")]
    [StringLength(60, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Busqueda { get; set; }

    public bool BusquedaRealizada { get; set; }

    public EquipoSeleccionableViewModel? Seleccionado { get; set; }

    public IReadOnlyList<EquipoSeleccionableViewModel> Resultados { get; set; } = [];
}
