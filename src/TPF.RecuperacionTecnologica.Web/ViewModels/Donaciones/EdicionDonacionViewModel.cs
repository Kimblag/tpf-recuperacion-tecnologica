using Microsoft.AspNetCore.Mvc.ModelBinding;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

public class EdicionDonacionViewModel : RegistroDonacionViewModel
{
    public int NroEquipo { get; set; }

    // agrego bind never para evitar que se modifiquen campos de solo lectura
    [BindNever]
    public string Donante { get; set; } = string.Empty;

    [BindNever]
    public DateTime FechaRegistro { get; set; }

    [BindNever]
    public EstadoEquipo EstadoEquipo { get; set; }

    public string CodigoEquipo => NroEquipo.ToString("D6");
}