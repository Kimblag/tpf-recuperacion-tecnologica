using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Parametros;

namespace TPF.RecuperacionTecnologica.Web.Services.Configuracion;

public class ConfiguracionService : IConfiguracionService
{
    private const int IdConfiguracion = 1;
    private const int MaximoDiasLimiteRetiro = 365;
    private const int MaximoSolicitudesPendientes = 20;

    private static IdentityResult Fallo(string codigo, string descripcion) =>
        IdentityResult.Failed(new IdentityError { Code = codigo, Description = descripcion });

    private readonly ApplicationDbContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<ConfiguracionService> _logger;

    public ConfiguracionService(
        ApplicationDbContext context,
        IAuditoriaService auditoriaService,
        ILogger<ConfiguracionService> logger)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _logger = logger;
    }

    public async Task<ConfiguracionInstitucional?> ObtenerAsync()
    {
        return await _context.ConfiguracionInstitucional
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == IdConfiguracion);
    }

    public async Task<IdentityResult> EditarAsync(string identityUserIdAdministrador, ParametrosViewModel modelo)
    {
        // las reglas se validan acá también: no se confía solo en el formulario
        if (modelo.DiasLimiteRetiro is not > 0 || modelo.DiasLimiteRetiro > MaximoDiasLimiteRetiro)
        {
            return Fallo("DiasLimiteRetiroInvalido",
                $"Los días límite de retiro deben estar entre 1 y {MaximoDiasLimiteRetiro}.");
        }

        if (modelo.MaximoSolicitudesPendientes is not > 0 || modelo.MaximoSolicitudesPendientes > MaximoSolicitudesPendientes)
        {
            return Fallo("MaximoSolicitudesPendientesInvalido",
                $"El máximo de solicitudes pendientes debe estar entre 1 y {MaximoSolicitudesPendientes}.");
        }

        // el aviso no puede superar el plazo de retiro: si no, toda asignación estaría por vencer desde el primer día
        if (modelo.DiasAvisoVencimiento is not > 0 || modelo.DiasAvisoVencimiento > modelo.DiasLimiteRetiro)
        {
            return Fallo("DiasAvisoVencimientoInvalido",
                "Los días de aviso antes del vencimiento deben estar entre 1 y los días límite de retiro.");
        }

        var codigoAdministrador = await _context.PersonalInterno
            .Where(p => p.IdentityUserId == identityUserIdAdministrador
                && p.RolAsignado == RolPersonal.Administrador
                && p.EstadoPersonal == EstadoPersonal.Activo)
            .Select(p => (int?)p.CodigoPersonal)
            .FirstOrDefaultAsync();

        if (codigoAdministrador is null)
        {
            return Fallo("AdministradorNoHabilitado", "La cuenta no está habilitada para modificar los parámetros.");
        }

        var configuracion = await _context.ConfiguracionInstitucional
            .FirstOrDefaultAsync(c => c.Id == IdConfiguracion);

        if (configuracion is null)
        {
            return Fallo("ConfiguracionNoEncontrada", "No se encontró la configuración institucional.");
        }

        // el RowVersion del formulario es el valor original: si otro Administrador guardó antes, no coincide
        _context.Entry(configuracion).Property(c => c.RowVersion).OriginalValue = modelo.RowVersion;

        configuracion.NombreOrganizacion = modelo.NombreOrganizacion.Trim();
        configuracion.ZonaCobertura = modelo.ZonaCobertura.Trim();
        configuracion.DiasLimiteRetiro = modelo.DiasLimiteRetiro.Value;
        configuracion.MaximoSolicitudesPendientes = modelo.MaximoSolicitudesPendientes.Value;
        configuracion.DiasAvisoVencimiento = modelo.DiasAvisoVencimiento.Value;
        configuracion.CodigoAdministrador = codigoAdministrador.Value;
        configuracion.FechaUltimaModificacion = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fallo("ConfiguracionModificada",
                "Otra persona modificó los parámetros mientras se editaban. Volver a abrir la pantalla e intentar nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la modificación de los parámetros institucionales");
            return Fallo("EdicionFallida", "No se pudo guardar el cambio. Intentar nuevamente.");
        }

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.ModificacionParametro, codigoAdministrador, nameof(ConfiguracionInstitucional),
                configuracion.Id, "Modificación de los parámetros institucionales");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar la modificación de los parámetros institucionales");
        }

        return IdentityResult.Success;
    }
}
