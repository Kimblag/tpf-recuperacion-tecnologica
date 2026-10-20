using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;

// las características son las verificadas en el diagnóstico vigente, no las declaradas por el donante
public class EquipoDisponibleViewModel
{
    public int NroEquipo { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public string? Procesador { get; set; }

    public string? MemoriaRam { get; set; }

    public string? Almacenamiento { get; set; }

    public string? TamanoPantalla { get; set; }

    public string? EstadoBateria { get; set; }

    public string? Otros { get; set; }

    public string? ImagenUrl { get; set; }

    public string CodigoEquipo => NroEquipo.ToString("D6");

    public string MarcaYModelo => $"{Marca} {Modelo}".Trim();

    public bool TieneEspecificaciones =>
        !string.IsNullOrWhiteSpace(Procesador)
        || !string.IsNullOrWhiteSpace(MemoriaRam)
        || !string.IsNullOrWhiteSpace(Almacenamiento)
        || !string.IsNullOrWhiteSpace(TamanoPantalla)
        || !string.IsNullOrWhiteSpace(EstadoBateria)
        || !string.IsNullOrWhiteSpace(Otros);
}
