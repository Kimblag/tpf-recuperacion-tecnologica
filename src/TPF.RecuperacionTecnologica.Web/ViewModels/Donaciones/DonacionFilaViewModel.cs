using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

public class DonacionFilaViewModel
{
    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public EstadoInicialDeclarado EstadoInicialDeclarado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public EstadoEquipo EstadoEquipo { get; set; }

    public string CodigoEquipo => NroEquipo.ToString("D6");

    public string MarcaYModelo => $"{Marca} {Modelo}".Trim();

    public bool PuedeModificarse => EstadoEquipo == EstadoEquipo.PendienteRecepcion;
}