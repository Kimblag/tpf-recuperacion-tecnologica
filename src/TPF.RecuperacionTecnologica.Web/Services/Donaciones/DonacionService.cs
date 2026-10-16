using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

namespace TPF.RecuperacionTecnologica.Web.Services.Donaciones;

public class DonacionService : IDonacionService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<DonacionService> _logger;

    public DonacionService(
        ApplicationDbContext context,
        IAuditoriaService auditoriaService,
        ILogger<DonacionService> logger)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _logger = logger;
    }

    // los opcionales vacíos se guardan como null para no llenar la base de textos en blanco
    private static string? TextoOpcional(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    public async Task<int?> RegistrarAsync(string identityUserId, RegistroDonacionViewModel modelo)
    {
        // solo dona un Usuario activo; el Personal no tiene ficha de Usuario
        var nroUsuario = await _context.Usuarios
            .Where(u => u.IdentityUserId == identityUserId && u.EstadoUsuario == EstadoUsuario.Activo)
            .Select(u => (int?)u.NroUsuario)
            .FirstOrDefaultAsync();

        if (nroUsuario is null)
        {
            return null;
        }

        var equipo = new Equipo
        {
            NroUsuarioDonante = nroUsuario.Value,
            TipoEquipo = modelo.TipoEquipo!.Value,
            Marca = modelo.Marca.Trim(),
            Modelo = modelo.Modelo.Trim(),
            EstadoInicialDeclarado = modelo.EstadoInicialDeclarado!.Value,
            Observaciones = TextoOpcional(modelo.Observaciones),
            EspecificacionesDeclaradas = new EspecificacionesTecnicas
            {
                Procesador = TextoOpcional(modelo.Procesador),
                MemoriaRam = TextoOpcional(modelo.MemoriaRam),
                Almacenamiento = TextoOpcional(modelo.Almacenamiento),
                TamanoPantalla = TextoOpcional(modelo.TamanoPantalla),
                EstadoBateria = TextoOpcional(modelo.EstadoBateria),
                Otros = TextoOpcional(modelo.Otros)
            },
            EstadoEquipo = EstadoEquipo.PendienteRecepcion,
            FechaRegistro = DateTime.UtcNow
        };

        _context.Equipos.Add(equipo);
        await _context.SaveChangesAsync();

        // la donación ya está guardada: si falla la auditoría se registra pero no la deshace
        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.Donacion, null, nameof(Equipo), equipo.NroEquipo,
                $"Donación de equipo ({equipo.TipoEquipo})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar la donación del Equipo {NroEquipo}", equipo.NroEquipo);
        }

        return equipo.NroEquipo;
    }
}
