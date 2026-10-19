using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Diagnosticos;

namespace TPF.RecuperacionTecnologica.Web.Services.Diagnosticos;

public class DiagnosticoService : IDiagnosticoService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<DiagnosticoService> _logger;

    public DiagnosticoService(
        ApplicationDbContext context,
        IAuditoriaService auditoriaService,
        ILogger<DiagnosticoService> logger)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _logger = logger;
    }

    // los opcionales vacíos se guardan como null para no llenar la base de textos en blanco
    private static string? TextoOpcional(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static IdentityResult Fallo(string codigo, string descripcion) =>
        IdentityResult.Failed(new IdentityError { Code = codigo, Description = descripcion });

    public async Task<IdentityResult> RegistrarInicialAsync(int codigoTecnico, DiagnosticoFormularioViewModel modelo)
    {
        if (modelo.DictamenTecnico is not { } dictamen)
        {
            return Fallo("DictamenRequerido", "El dictamen técnico es obligatorio.");
        }

        var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.NroEquipo == modelo.NroEquipo);

        if (equipo is null) return Fallo("EquipoNoEncontrado", "No se encontró el equipo.");

        // el estado se verifica de nuevo para no diagnosticar un equipo que cambió desde que se abrió el formulario
        if (equipo.EstadoEquipo != EstadoEquipo.PendienteRecepcion) return Fallo("EquipoNoDiagnosticable",
                "El equipo ya no está pendiente de recepción, por lo que no admite un diagnóstico inicial.");

        var yaDiagnosticado = await _context.Diagnosticos
            .AnyAsync(d => d.NroEquipo == equipo.NroEquipo && d.TipoDiagnostico == TipoDiagnostico.Inicial);

        if (yaDiagnosticado) return Fallo("DiagnosticoInicialExistente", "El equipo ya tiene un diagnóstico inicial.");

        var ahora = DateTime.UtcNow;

        var diagnostico = new Diagnostico
        {
            NroEquipo = equipo.NroEquipo,
            CodigoTecnico = codigoTecnico,
            TipoDiagnostico = TipoDiagnostico.Inicial,
            FechaDiagnostico = ahora,
            DictamenTecnico = dictamen,
            DetalleFallas = TextoOpcional(modelo.DetalleFallas),
            Observaciones = TextoOpcional(modelo.Observaciones),
            EspecificacionesVerificadas = new EspecificacionesTecnicas
            {
                Procesador = TextoOpcional(modelo.Procesador),
                MemoriaRam = TextoOpcional(modelo.MemoriaRam),
                Almacenamiento = TextoOpcional(modelo.Almacenamiento),
                TamanoPantalla = TextoOpcional(modelo.TamanoPantalla),
                EstadoBateria = TextoOpcional(modelo.EstadoBateria),
                Otros = TextoOpcional(modelo.Otros)
            },
            EsVigente = true
        };

        equipo.EstadoEquipo = dictamen switch
        {
            DictamenTecnico.Funcional => EstadoEquipo.Disponible,
            DictamenTecnico.NecesitaReparacion => EstadoEquipo.EnReparacion,
            _ => EstadoEquipo.Baja
        };

        if (dictamen == DictamenTecnico.Funcional)
        {
            equipo.FechaDisponibilidad = ahora;
        }

        equipo.FechaUltimaModificacion = ahora;

        _context.Diagnosticos.Add(diagnostico);

        // el diagnóstico y el cambio de estado se guardan juntos en una sola operación
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fallo("EquipoModificado",
                "El equipo cambió mientras se registraba el diagnóstico. Vuelva a abrirlo e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el registro del diagnóstico inicial del Equipo {NroEquipo}", equipo.NroEquipo);
            return Fallo("RegistroFallido", "No se pudo registrar el diagnóstico. Intente nuevamente.");
        }

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.Diagnostico, codigoTecnico, nameof(Diagnostico), diagnostico.NroDiagnostico,
                $"Diagnóstico inicial del equipo {equipo.NroEquipo}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar el diagnóstico inicial del Equipo {NroEquipo}", equipo.NroEquipo);
        }

        return IdentityResult.Success;
    }
}
