using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

public class CancelarDonacionViewModel
{
    public int NroEquipo { get; set; }


    // para evitar que se modifiquen campos de solo lectura
    [BindNever]
    public string CodigoEquipo { get; set; } = string.Empty;

    [BindNever]
    public string MarcaYModelo { get; set; } = string.Empty;
}