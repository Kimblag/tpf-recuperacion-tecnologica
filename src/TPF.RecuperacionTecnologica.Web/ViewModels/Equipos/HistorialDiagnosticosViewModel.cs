using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

public class HistorialDiagnosticosViewModel
{
    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public EstadoEquipo EstadoEquipo { get; set; }

    public DiagnosticoVigenteViewModel? Vigente { get; set; }

    public IReadOnlyList<DiagnosticoFilaViewModel> Diagnosticos { get; set; } = [];

    public string CodigoEquipo => NroEquipo.ToString("D6");

    public string MarcaYModelo => $"{Marca} {Modelo}".Trim();
}
