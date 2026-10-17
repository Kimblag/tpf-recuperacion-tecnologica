using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

public class DiagnosticoFilaViewModel
{
    public int NroDiagnostico { get; set; }

    public int NroEquipo { get; set; }

    public TipoDiagnostico TipoDiagnostico { get; set; }

    public DateTime FechaDiagnostico { get; set; }

    public int CodigoTecnico { get; set; }

    public DictamenTecnico DictamenTecnico { get; set; }

    public bool EsVigente { get; set; }

    public int? NroDiagnosticoRectificado { get; set; }

    public int? NroDiagnosticoReemplazante { get; set; }

    public string CodigoDiagnostico => $"DIA-{NroDiagnostico:D6}";

    public string CodigoEquipo => NroEquipo.ToString("D6");

    public string CodigoPersonal => $"PER-{CodigoTecnico:D4}";
}
