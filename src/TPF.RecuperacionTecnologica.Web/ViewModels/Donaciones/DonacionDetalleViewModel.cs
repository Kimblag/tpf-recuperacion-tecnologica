using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

public class DonacionDetalleViewModel : DonacionFilaViewModel
{
    public string Donante { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public string? Procesador { get; set; }

    public string? MemoriaRam { get; set; }

    public string? Almacenamiento { get; set; }

    public string? TamanoPantalla { get; set; }

    public string? EstadoBateria { get; set; }

    public string? Otros { get; set; }

    public IReadOnlyList<HitoViewModel> Historial { get; set; } = [];

    public bool PuedeModificarse => EstadoEquipo == EstadoEquipo.PendienteRecepcion;

    public bool EspecificacionesVerificadas { get; set; }

    public bool TieneEspecificaciones =>
        !string.IsNullOrWhiteSpace(Procesador)
        || !string.IsNullOrWhiteSpace(MemoriaRam)
        || !string.IsNullOrWhiteSpace(Almacenamiento)
        || !string.IsNullOrWhiteSpace(TamanoPantalla)
        || !string.IsNullOrWhiteSpace(EstadoBateria)
        || !string.IsNullOrWhiteSpace(Otros);
}