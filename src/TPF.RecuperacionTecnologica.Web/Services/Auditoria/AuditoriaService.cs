using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.Services.Auditoria;

public class AuditoriaService : IAuditoriaService
{
    private readonly ApplicationDbContext _context;
    private const int DetalleMaxLength = 500;

    public AuditoriaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarEventoAsync(TipoOperacionAuditoria tipoOperacion, int? codigoPersonal, string entidadAfectada, int idEntidadAfectada, string? detalle)
    {
        // Limpiar y normalizar el detalle antes de guardarlo
        string? detalleNormalizado = detalle?.Trim();
        // si tine espacios, guardar null
        if (string.IsNullOrWhiteSpace(detalleNormalizado)) detalleNormalizado = null;

        // Si hay verificar su longitud antes de recortar a 500 caracteres
        if (detalleNormalizado != null && detalleNormalizado.Length > DetalleMaxLength)
        {
            detalleNormalizado = detalleNormalizado.Substring(0, DetalleMaxLength);
        }

        var registro = new RegistroAuditoria()
        {
            FechaHora = DateTime.UtcNow,
            TipoOperacion = tipoOperacion,
            CodigoPersonal = codigoPersonal,
            EntidadAfectada = entidadAfectada,
            IdEntidadAfectada = idEntidadAfectada,
            Detalle = detalleNormalizado
        };

        _context.RegistrosAuditoria.Add(registro);
        await _context.SaveChangesAsync();
    }
}

