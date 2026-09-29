using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.Models;

public class Equipo
{
    public int NroEquipo { get; set; }

    public int NroUsuarioDonante { get; set; }

    public Usuario UsuarioDonante { get; set; } = null!;

    public TipoEquipo TipoEquipo { get; set; }

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public EstadoInicialDeclarado EstadoInicialDeclarado { get; set; }

    public string? Observaciones { get; set; }

    public EspecificacionesTecnicas EspecificacionesDeclaradas { get; set; } = new();

    public EstadoEquipo EstadoEquipo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaUltimaModificacion { get; set; }

    public DateTime? FechaDisponibilidad { get; set; }

    public DateTime? FechaCancelacion { get; set; }

    public string? MotivoCancelacion { get; set; }

    public string? ImagenUrl { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
