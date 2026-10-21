using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Diagnosticos;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

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

    public async Task<ResultadoPaginado<DiagnosticoFilaViewModel>> ListarAsync(
        string? busqueda, TipoDiagnostico? tipo, DictamenTecnico? dictamen, int pagina, int tamanoPagina)
    {
        var consulta = _context.Diagnosticos.AsNoTracking();

        if (tipo is not null)
        {
            consulta = consulta.Where(d => d.TipoDiagnostico == tipo);
        }

        if (dictamen is not null)
        {
            consulta = consulta.Where(d => d.DictamenTecnico == dictamen);
        }

        var texto = busqueda?.Trim();

        if (!string.IsNullOrEmpty(texto))
        {
            // el código de diagnóstico se muestra como DIA-000123, así que se acepta con o sin el prefijo
            if (texto.StartsWith("DIA-", StringComparison.OrdinalIgnoreCase))
            {
                texto = texto[4..];
            }

            consulta = int.TryParse(texto, out var numero)
                ? consulta.Where(d => d.NroDiagnostico == numero || d.NroEquipo == numero)
                : consulta.Where(d => false);
        }

        var total = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var elementos = await consulta
            .OrderByDescending(d => d.FechaDiagnostico)
            .ThenByDescending(d => d.NroDiagnostico)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(d => new DiagnosticoFilaViewModel
            {
                NroDiagnostico = d.NroDiagnostico,
                NroEquipo = d.NroEquipo,
                TipoDiagnostico = d.TipoDiagnostico,
                FechaDiagnostico = d.FechaDiagnostico,
                CodigoTecnico = d.CodigoTecnico,
                DictamenTecnico = d.DictamenTecnico,
                EsVigente = d.EsVigente,
                NroDiagnosticoRectificado = d.DiagnosticoRectificadoId,
                NroDiagnosticoReemplazante = _context.Diagnosticos
                    .Where(r => r.DiagnosticoRectificadoId == d.NroDiagnostico)
                    .Select(r => (int?)r.NroDiagnostico)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return new ResultadoPaginado<DiagnosticoFilaViewModel>
        {
            Elementos = elementos,
            Total = total,
            Pagina = pagina
        };
    }

    public async Task<DiagnosticoDetalleViewModel?> ObtenerDetalleAsync(int nroDiagnostico) =>
        await _context.Diagnosticos
            .AsNoTracking()
            .Where(d => d.NroDiagnostico == nroDiagnostico)
            .Select(d => new DiagnosticoDetalleViewModel
            {
                NroDiagnostico = d.NroDiagnostico,
                TipoDiagnostico = d.TipoDiagnostico,
                FechaDiagnostico = d.FechaDiagnostico,
                NroEquipo = d.NroEquipo,
                TipoEquipo = d.Equipo.TipoEquipo,
                Marca = d.Equipo.Marca,
                Modelo = d.Equipo.Modelo,
                EstadoEquipo = d.Equipo.EstadoEquipo,
                CodigoTecnico = d.CodigoTecnico,
                Tecnico = d.Tecnico.Nombre + " " + d.Tecnico.Apellido,
                DictamenTecnico = d.DictamenTecnico,
                EsVigente = d.EsVigente,
                NroDiagnosticoRectificado = d.DiagnosticoRectificadoId,
                NroDiagnosticoReemplazante = _context.Diagnosticos
                    .Where(r => r.DiagnosticoRectificadoId == d.NroDiagnostico)
                    .Select(r => (int?)r.NroDiagnostico)
                    .FirstOrDefault(),
                DetalleFallas = d.DetalleFallas,
                DetalleReparacionesRealizadas = d.DetalleReparacionesRealizadas,
                Observaciones = d.Observaciones,
                Procesador = d.EspecificacionesVerificadas.Procesador,
                MemoriaRam = d.EspecificacionesVerificadas.MemoriaRam,
                Almacenamiento = d.EspecificacionesVerificadas.Almacenamiento,
                TamanoPantalla = d.EspecificacionesVerificadas.TamanoPantalla,
                EstadoBateria = d.EspecificacionesVerificadas.EstadoBateria,
                Otros = d.EspecificacionesVerificadas.Otros
            })
            .FirstOrDefaultAsync();

    // los opcionales vacíos se guardan como null para no llenar la base de textos en blanco
    private static string? TextoOpcional(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static IdentityResult Fallo(string codigo, string descripcion) =>
        IdentityResult.Failed(new IdentityError { Code = codigo, Description = descripcion });

    private static Diagnostico ArmarDiagnostico(
        Equipo equipo, int codigoTecnico, TipoDiagnostico tipo, DictamenTecnico dictamen,
        DiagnosticoFormularioViewModel modelo, DateTime fecha) => new()
        {
            NroEquipo = equipo.NroEquipo,
            CodigoTecnico = codigoTecnico,
            TipoDiagnostico = tipo,
            FechaDiagnostico = fecha,
            DictamenTecnico = dictamen,
            DetalleFallas = TextoOpcional(modelo.DetalleFallas),
            DetalleReparacionesRealizadas = TextoOpcional(modelo.DetalleReparacionesRealizadas),
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

    // el estado del equipo sigue al dictamen; un equipo que pasa a Disponible registra desde cuándo lo está
    private static void AplicarDictamen(Equipo equipo, DictamenTecnico dictamen, DateTime fecha)
    {
        // una rectificación sobre un equipo que ya estaba Disponible no cambia desde cuándo lo está
        if (dictamen == DictamenTecnico.Funcional && equipo.EstadoEquipo != EstadoEquipo.Disponible)
        {
            equipo.FechaDisponibilidad = fecha;
        }

        equipo.EstadoEquipo = dictamen switch
        {
            DictamenTecnico.Funcional => EstadoEquipo.Disponible,
            DictamenTecnico.NecesitaReparacion => EstadoEquipo.EnReparacion,
            _ => EstadoEquipo.Baja
        };

        equipo.FechaUltimaModificacion = fecha;
    }

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
        var diagnostico = ArmarDiagnostico(equipo, codigoTecnico, TipoDiagnostico.Inicial, dictamen, modelo, ahora);

        AplicarDictamen(equipo, dictamen, ahora);
        _context.Diagnosticos.Add(diagnostico);

        return await GuardarYAuditarAsync(equipo, diagnostico, codigoTecnico, "Diagnóstico inicial");
    }

    public async Task<IdentityResult> RegistrarSegundoAsync(int codigoTecnico, DiagnosticoFormularioViewModel modelo)
    {
        if (modelo.DictamenTecnico is not { } dictamen)
        {
            return Fallo("DictamenRequerido", "El dictamen técnico es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(modelo.DetalleReparacionesRealizadas))
        {
            return Fallo("DetalleReparacionRequerido", "El detalle de reparación es obligatorio en el segundo diagnóstico.");
        }

        var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.NroEquipo == modelo.NroEquipo);

        if (equipo is null) return Fallo("EquipoNoEncontrado", "No se encontró el equipo.");

        // el estado se verifica de nuevo para no diagnosticar un equipo que cambió desde que se abrió el formulario
        if (equipo.EstadoEquipo != EstadoEquipo.EnReparacion) return Fallo("EquipoNoDiagnosticable",
                "El equipo ya no está en reparación, por lo que no admite un segundo diagnóstico.");

        var ahora = DateTime.UtcNow;

        // el diagnóstico que estaba vigente pasa a ser histórico
        var vigentes = await _context.Diagnosticos
            .Where(d => d.NroEquipo == equipo.NroEquipo && d.EsVigente)
            .ToListAsync();

        foreach (var anterior in vigentes)
        {
            anterior.EsVigente = false;
        }

        var diagnostico = ArmarDiagnostico(equipo, codigoTecnico, TipoDiagnostico.Segundo, dictamen, modelo, ahora);

        AplicarDictamen(equipo, dictamen, ahora);
        _context.Diagnosticos.Add(diagnostico);

        return await GuardarYAuditarAsync(equipo, diagnostico, codigoTecnico, "Segundo diagnóstico");
    }

    public async Task<IdentityResult> RegistrarRectificacionAsync(int codigoTecnico, DiagnosticoFormularioViewModel modelo)
    {
        if (modelo.DictamenTecnico is not { } dictamen)
        {
            return Fallo("DictamenRequerido", "El dictamen técnico es obligatorio.");
        }

        var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.NroEquipo == modelo.NroEquipo);

        if (equipo is null) return Fallo("EquipoNoEncontrado", "No se encontró el equipo.");

        // un equipo asignado o entregado ya salió del circuito de diagnóstico: se mantiene el dictamen vigente
        if (equipo.EstadoEquipo is EstadoEquipo.Asignado or EstadoEquipo.Entregado) return Fallo("EquipoNoRectificable",
                "El equipo ya fue asignado o entregado. Su diagnóstico no se puede rectificar.");

        if (equipo.EstadoEquipo is not (EstadoEquipo.Disponible or EstadoEquipo.EnReparacion or EstadoEquipo.Baja))
            return Fallo("EquipoNoRectificable", "El estado actual del equipo no admite rectificar su diagnóstico.");

        var vigente = await _context.Diagnosticos
            .FirstOrDefaultAsync(d => d.NroEquipo == equipo.NroEquipo && d.EsVigente);

        if (vigente is null) return Fallo("SinDiagnosticoVigente", "El equipo no tiene un diagnóstico vigente para rectificar.");

        var ahora = DateTime.UtcNow;

        // el diagnóstico original queda en el historial como no vigente
        vigente.EsVigente = false;

        var diagnostico = ArmarDiagnostico(equipo, codigoTecnico, TipoDiagnostico.Rectificacion, dictamen, modelo, ahora);
        diagnostico.DiagnosticoRectificadoId = vigente.NroDiagnostico;

        AplicarDictamen(equipo, dictamen, ahora);
        _context.Diagnosticos.Add(diagnostico);

        return await GuardarYAuditarAsync(equipo, diagnostico, codigoTecnico,
            $"Rectificación del diagnóstico {vigente.NroDiagnostico}");
    }

    // el diagnóstico y el cambio de estado se guardan juntos en una sola operación
    private async Task<IdentityResult> GuardarYAuditarAsync(
        Equipo equipo, Diagnostico diagnostico, int codigoTecnico, string descripcion)
    {
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
            _logger.LogError(ex, "Falló el registro del diagnóstico {Tipo} del Equipo {NroEquipo}",
                diagnostico.TipoDiagnostico, equipo.NroEquipo);
            return Fallo("RegistroFallido", "No se pudo registrar el diagnóstico. Intente nuevamente.");
        }

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.Diagnostico, codigoTecnico, nameof(Diagnostico), diagnostico.NroDiagnostico,
                $"{descripcion} del equipo {equipo.NroEquipo}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar el diagnóstico {Tipo} del Equipo {NroEquipo}",
                diagnostico.TipoDiagnostico, equipo.NroEquipo);
        }

        return IdentityResult.Success;
    }
}
