using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

public class FotoGestionViewModel
{
    public int NroImagen { get; set; }

    public string UrlImagen { get; set; } = string.Empty;

    public string? Angulo { get; set; }

    public int? NroDiagnostico { get; set; }
}

public class GestionFotosViewModel
{
    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public EstadoEquipo EstadoEquipo { get; set; }

    public IReadOnlyList<FotoGestionViewModel> Fotos { get; set; } = [];

    public bool PuedeEditar { get; set; }

    public string? MotivoNoEditable { get; set; }

    public int MaximoPorEquipo { get; set; }

    public int MaximoPorEnvio { get; set; }

    public long TamanoMaximoBytes { get; set; }

    public string CodigoEquipo => NroEquipo.ToString("D6");

    public string MarcaYModelo => $"{Marca} {Modelo}".Trim();

    public int Disponibles => Math.Max(0, MaximoPorEquipo - Fotos.Count);

    public bool EsUltimaFotoProtegida => EstadoEquipo == EstadoEquipo.Disponible && Fotos.Count == 1;
}