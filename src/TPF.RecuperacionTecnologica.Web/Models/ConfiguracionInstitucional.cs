namespace TPF.RecuperacionTecnologica.Web.Models;

public class ConfiguracionInstitucional
{
    public int Id { get; set; } = 1;

    public string NombreOrganizacion { get; set; } = string.Empty;

    public string ZonaCobertura { get; set; } = string.Empty;

    public int DiasLimiteRetiro { get; set; }

    public int CodigoAdministrador { get; set; }

    public Personal Administrador { get; set; } = null!;

    public DateTime? FechaUltimaModificacion { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
