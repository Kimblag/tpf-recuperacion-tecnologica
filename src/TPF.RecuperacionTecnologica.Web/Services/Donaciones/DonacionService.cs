using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
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

    private static IdentityResult Fallo(string codigo, string descripcion) =>
        IdentityResult.Failed(new IdentityError { Code = codigo, Description = descripcion });

    private const string MotivoCancelacionDonante = "Cancelada por el donante";

    private static EspecificacionesTecnicas EspecificacionesDelTipo(TipoEquipo tipo, RegistroDonacionViewModel modelo)
    {
        var aplicaProcesador = tipo is TipoEquipo.Notebook or TipoEquipo.PcEscritorio;
        var aplicaMemoriaYAlmacenamiento = tipo is TipoEquipo.Notebook or TipoEquipo.PcEscritorio or TipoEquipo.Celular;
        var aplicaPantalla = tipo is TipoEquipo.Notebook or TipoEquipo.Monitor;
        var aplicaBateria = tipo is TipoEquipo.Notebook or TipoEquipo.Celular;

        return new EspecificacionesTecnicas
        {
            Procesador = aplicaProcesador ? TextoOpcional(modelo.Procesador) : null,
            MemoriaRam = aplicaMemoriaYAlmacenamiento ? TextoOpcional(modelo.MemoriaRam) : null,
            Almacenamiento = aplicaMemoriaYAlmacenamiento ? TextoOpcional(modelo.Almacenamiento) : null,
            TamanoPantalla = aplicaPantalla ? TextoOpcional(modelo.TamanoPantalla) : null,
            EstadoBateria = aplicaBateria ? TextoOpcional(modelo.EstadoBateria) : null,
            Otros = TextoOpcional(modelo.Otros)
        };
    }

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
            EspecificacionesDeclaradas = EspecificacionesDelTipo(modelo.TipoEquipo!.Value, modelo),
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

    public async Task<EdicionDonacionViewModel?> ObtenerParaEdicionAsync(string identityUserId, int nroEquipo)
    {
        // la consulta filtra por la cuenta: una donación ajena es igual que una inexistente
        var datos = await _context.Equipos
            .AsNoTracking()
            .Where(e => e.NroEquipo == nroEquipo
                && e.UsuarioDonante.IdentityUserId == identityUserId
                && e.UsuarioDonante.EstadoUsuario == EstadoUsuario.Activo)
            .Select(e => new
            {
                e.NroEquipo,
                e.TipoEquipo,
                e.Marca,
                e.Modelo,
                e.EstadoInicialDeclarado,
                e.Observaciones,
                e.EstadoEquipo,
                e.FechaRegistro,
                e.EspecificacionesDeclaradas.Procesador,
                e.EspecificacionesDeclaradas.MemoriaRam,
                e.EspecificacionesDeclaradas.Almacenamiento,
                e.EspecificacionesDeclaradas.TamanoPantalla,
                e.EspecificacionesDeclaradas.EstadoBateria,
                e.EspecificacionesDeclaradas.Otros,
                e.UsuarioDonante.TipoUsuario,
                e.UsuarioDonante.Nombre,
                e.UsuarioDonante.Apellido,
                e.UsuarioDonante.RazonSocial
            })
            .FirstOrDefaultAsync();

        if (datos is null)
        {
            return null;
        }

        var donante = datos.TipoUsuario == TipoUsuario.Persona
            ? $"{datos.Nombre} {datos.Apellido}".Trim()
            : datos.RazonSocial ?? string.Empty;

        return new EdicionDonacionViewModel
        {
            NroEquipo = datos.NroEquipo,
            Donante = donante,
            FechaRegistro = datos.FechaRegistro,
            EstadoEquipo = datos.EstadoEquipo,
            TipoEquipo = datos.TipoEquipo,
            Marca = datos.Marca,
            Modelo = datos.Modelo,
            EstadoInicialDeclarado = datos.EstadoInicialDeclarado,
            Observaciones = datos.Observaciones,
            Procesador = datos.Procesador,
            MemoriaRam = datos.MemoriaRam,
            Almacenamiento = datos.Almacenamiento,
            TamanoPantalla = datos.TamanoPantalla,
            EstadoBateria = datos.EstadoBateria,
            Otros = datos.Otros
        };
    }

    public async Task<IdentityResult> EditarAsync(string identityUserId, EdicionDonacionViewModel modelo)
    {
        var equipo = await _context.Equipos
            .FirstOrDefaultAsync(e => e.NroEquipo == modelo.NroEquipo
                && e.UsuarioDonante.IdentityUserId == identityUserId
                && e.UsuarioDonante.EstadoUsuario == EstadoUsuario.Activo);

        // no se distingue entre una donación inexistente y una de otro Usuario
        if (equipo is null)
        {
            return Fallo("DonacionNoEncontrada", "No se encontró la donación.");
        }

        if (equipo.EstadoEquipo != EstadoEquipo.PendienteRecepcion)
        {
            return Fallo("DonacionNoEditable",
                "La donación ya no puede modificarse porque el equipo fue recibido o su estado cambió.");
        }

        // el tipo de equipo no se toca: si el donante se equivocó, cancela y registra una nueva
        equipo.Marca = modelo.Marca.Trim();
        equipo.Modelo = modelo.Modelo.Trim();
        equipo.EstadoInicialDeclarado = modelo.EstadoInicialDeclarado!.Value;
        equipo.Observaciones = TextoOpcional(modelo.Observaciones);
        equipo.EspecificacionesDeclaradas = EspecificacionesDelTipo(equipo.TipoEquipo, modelo);
        equipo.FechaUltimaModificacion = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // el RowVersion cambió entre la lectura y el guardado
            return Fallo("DonacionModificada",
                "La donación cambió mientras se guardaba. Vuelva a abrirla e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la edición de la donación del Equipo {NroEquipo}", equipo.NroEquipo);
            return Fallo("EdicionFallida", "No se pudieron guardar los cambios. Intente nuevamente.");
        }

        // la edición ya está guardada: si falla la auditoría se registra pero no la deshace
        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.Donacion, null, nameof(Equipo), equipo.NroEquipo,
                "Edición de donación");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar la edición de la donación del Equipo {NroEquipo}", equipo.NroEquipo);
        }

        return IdentityResult.Success;
    }

    public async Task<ResultadoPaginado<DonacionFilaViewModel>> ListarAsync(
        string identityUserId, string? busqueda, EstadoEquipo? estado, TipoEquipo? tipo,
        EstadoInicialDeclarado? condicion, OrdenDonacion orden, int pagina, int tamanoPagina)
    {
        var consulta = _context.Equipos
            .AsNoTracking() // no se van a modificar los objetos, solo leerlos
            .Where(e => e.UsuarioDonante.IdentityUserId == identityUserId);

        if (estado is not null)
        {
            consulta = consulta.Where(e => e.EstadoEquipo == estado);
        }

        if (tipo is not null)
        {
            consulta = consulta.Where(e => e.TipoEquipo == tipo);
        }

        if (condicion is not null)
        {
            consulta = consulta.Where(e => e.EstadoInicialDeclarado == condicion);
        }

        var texto = busqueda?.Trim();

        if (!string.IsNullOrEmpty(texto))
        {
            consulta = int.TryParse(texto, out var nroEquipo)
                ? consulta.Where(e => e.NroEquipo == nroEquipo)
                : consulta.Where(e => e.Marca.Contains(texto) || e.Modelo.Contains(texto));
        }

        var total = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        Expression<Func<Equipo, int>> rangoTipo = e =>
            e.TipoEquipo == TipoEquipo.Celular ? 0
            : e.TipoEquipo == TipoEquipo.Monitor ? 1
            : e.TipoEquipo == TipoEquipo.Notebook ? 2
            : e.TipoEquipo == TipoEquipo.PcEscritorio ? 3
            : 4;

        Expression<Func<Equipo, int>> rangoCondicion = e =>
            e.EstadoInicialDeclarado == EstadoInicialDeclarado.ConFallas ? 0
            : e.EstadoInicialDeclarado == EstadoInicialDeclarado.Funcional ? 1
            : 2;

        IOrderedQueryable<Equipo> ordenada = orden switch
        {
            OrdenDonacion.RegistroAsc => consulta.OrderBy(e => e.FechaRegistro),
            OrdenDonacion.EquipoAsc => consulta.OrderBy(e => e.NroEquipo),
            OrdenDonacion.EquipoDesc => consulta.OrderByDescending(e => e.NroEquipo),
            OrdenDonacion.TipoAsc => consulta.OrderBy(rangoTipo),
            OrdenDonacion.TipoDesc => consulta.OrderByDescending(rangoTipo),
            OrdenDonacion.ModeloAsc => consulta.OrderBy(e => e.Marca).ThenBy(e => e.Modelo),
            OrdenDonacion.ModeloDesc => consulta.OrderByDescending(e => e.Marca).ThenByDescending(e => e.Modelo),
            OrdenDonacion.CondicionAsc => consulta.OrderBy(rangoCondicion),
            OrdenDonacion.CondicionDesc => consulta.OrderByDescending(rangoCondicion),
            OrdenDonacion.EstadoAsc => consulta.OrderBy(e => e.EstadoEquipo),
            OrdenDonacion.EstadoDesc => consulta.OrderByDescending(e => e.EstadoEquipo),
            _ => consulta.OrderByDescending(e => e.FechaRegistro)
        };

        var elementos = await ordenada
            .ThenByDescending(e => e.NroEquipo)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(e => new DonacionFilaViewModel
            {
                NroEquipo = e.NroEquipo,
                TipoEquipo = e.TipoEquipo,
                Marca = e.Marca,
                Modelo = e.Modelo,
                EstadoInicialDeclarado = e.EstadoInicialDeclarado,
                FechaRegistro = e.FechaRegistro,
                EstadoEquipo = e.EstadoEquipo
            })
            .ToListAsync();

        return new ResultadoPaginado<DonacionFilaViewModel>
        {
            Elementos = elementos,
            Total = total,
            Pagina = pagina
        };
    }

    public async Task<DonacionDetalleViewModel?> ObtenerDetalleAsync(string identityUserId, int nroEquipo)
    {
        var equipo = await _context.Equipos
            .AsNoTracking()
            .Where(e => e.NroEquipo == nroEquipo && e.UsuarioDonante.IdentityUserId == identityUserId)
            .Select(e => new
            {
                e.NroEquipo,
                e.TipoEquipo,
                e.Marca,
                e.Modelo,
                e.EstadoInicialDeclarado,
                e.Observaciones,
                e.EstadoEquipo,
                e.FechaRegistro,
                e.FechaCancelacion,
                e.EspecificacionesDeclaradas.Procesador,
                e.EspecificacionesDeclaradas.MemoriaRam,
                e.EspecificacionesDeclaradas.Almacenamiento,
                e.EspecificacionesDeclaradas.TamanoPantalla,
                e.EspecificacionesDeclaradas.EstadoBateria,
                e.EspecificacionesDeclaradas.Otros,
                e.UsuarioDonante.TipoUsuario,
                e.UsuarioDonante.Nombre,
                e.UsuarioDonante.Apellido,
                e.UsuarioDonante.RazonSocial
            })
            .FirstOrDefaultAsync();

        if (equipo is null)
        {
            return null;
        }

        var diagnostico = await _context.Diagnosticos
            .AsNoTracking()
            .Where(d => d.NroEquipo == nroEquipo && d.EsVigente)
            .Select(d => new
            {
                d.FechaDiagnostico,
                d.DictamenTecnico,
                d.EspecificacionesVerificadas.Procesador,
                d.EspecificacionesVerificadas.MemoriaRam,
                d.EspecificacionesVerificadas.Almacenamiento,
                d.EspecificacionesVerificadas.TamanoPantalla,
                d.EspecificacionesVerificadas.EstadoBateria,
                d.EspecificacionesVerificadas.Otros
            })
            .FirstOrDefaultAsync();

        var fechaAsignacion = await _context.Asignaciones
            .AsNoTracking()
            .Where(a => a.NroEquipo == nroEquipo && a.EstadoAsignacion != EstadoAsignacion.Liberada)
            .OrderByDescending(a => a.FechaAsignacion)
            .Select(a => (DateTime?)a.FechaAsignacion)
            .FirstOrDefaultAsync();

        var fechaEntrega = await _context.Entregas
            .AsNoTracking()
            .Where(e => e.NroEquipo == nroEquipo)
            .Select(e => (DateTime?)e.FechaEntrega)
            .FirstOrDefaultAsync();

        var donante = equipo.TipoUsuario == TipoUsuario.Persona
            ? $"{equipo.Nombre} {equipo.Apellido}".Trim()
            : equipo.RazonSocial ?? string.Empty;

        var verificadas = diagnostico is not null;

        return new DonacionDetalleViewModel
        {
            NroEquipo = equipo.NroEquipo,
            TipoEquipo = equipo.TipoEquipo,
            Marca = equipo.Marca,
            Modelo = equipo.Modelo,
            EstadoInicialDeclarado = equipo.EstadoInicialDeclarado,
            FechaRegistro = equipo.FechaRegistro,
            EstadoEquipo = equipo.EstadoEquipo,
            Donante = donante,
            Observaciones = equipo.Observaciones,
            EspecificacionesVerificadas = verificadas,
            Procesador = verificadas ? diagnostico!.Procesador : equipo.Procesador,
            MemoriaRam = verificadas ? diagnostico!.MemoriaRam : equipo.MemoriaRam,
            Almacenamiento = verificadas ? diagnostico!.Almacenamiento : equipo.Almacenamiento,
            TamanoPantalla = verificadas ? diagnostico!.TamanoPantalla : equipo.TamanoPantalla,
            EstadoBateria = verificadas ? diagnostico!.EstadoBateria : equipo.EstadoBateria,
            Otros = verificadas ? diagnostico!.Otros : equipo.Otros,
            Historial = ArmarHistorial(
                equipo.EstadoEquipo, equipo.EstadoInicialDeclarado, equipo.FechaRegistro, equipo.FechaCancelacion,
                diagnostico?.DictamenTecnico, diagnostico?.FechaDiagnostico, fechaAsignacion, fechaEntrega)
        };
    }

    public async Task<IdentityResult> CancelarAsync(string identityUserId, int nroEquipo)
    {
        // buscar el equipo y verificar que sea del Usuario y esté Pendiente de recepción
        var equipo = await _context.Equipos
            .FirstOrDefaultAsync(e => e.NroEquipo == nroEquipo
            && e.UsuarioDonante.IdentityUserId == identityUserId
            && e.UsuarioDonante.EstadoUsuario == EstadoUsuario.Activo);

        if (equipo is null) return Fallo("DonacionNoEncontrada", "No se encontró la donación.");

        if (equipo.EstadoEquipo != EstadoEquipo.PendienteRecepcion) return Fallo("DonacionNoCancelable",
                "La donación ya no puede cancelarse porque el equipo fue recibido o su estado cambió.");

        var ahora = DateTime.UtcNow;

        equipo.EstadoEquipo = EstadoEquipo.Cancelado;
        equipo.FechaCancelacion = ahora;
        equipo.MotivoCancelacion = MotivoCancelacionDonante;
        equipo.FechaUltimaModificacion = ahora;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Fallo("DonacionModificada",
                "La donación cambió mientras se cancelaba. Vuelva a abrirla e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la cancelación de la donación del Equipo {NroEquipo}", equipo.NroEquipo);
            return Fallo("CancelacionFallida", "No se pudo cancelar la donación. Intente nuevamente.");
        }

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.Donacion, null, nameof(Equipo), equipo.NroEquipo,
                "Cancelación de donación");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar la cancelación de la donación del Equipo {NroEquipo}", equipo.NroEquipo);
        }

        return IdentityResult.Success;
    }


    private static IReadOnlyList<HitoViewModel> ArmarHistorial(
        EstadoEquipo estado, EstadoInicialDeclarado condicion, DateTime fechaRegistro, DateTime? fechaCancelacion,
        DictamenTecnico? dictamen, DateTime? fechaDiagnostico, DateTime? fechaAsignacion, DateTime? fechaEntrega)
    {
        var condicionTexto = condicion switch
        {
            EstadoInicialDeclarado.ConFallas => "con fallas",
            EstadoInicialDeclarado.ParaRepuestos => "para repuestos",
            _ => "funcional"
        };

        var registrada = Hito("Donación registrada", fechaRegistro, "gift", EstadoHito.Completado,
            $"Estado inicial declarado: {condicionTexto}.");

        if (estado == EstadoEquipo.Cancelado)
        {
            return
            [
                registrada,
                Hito("Donación cancelada", fechaCancelacion, "x", EstadoHito.Cancelado,
                    "Cancelada por el donante antes de la recepción.")
            ];
        }

        var dictamenTexto = dictamen switch
        {
            DictamenTecnico.NecesitaReparacion => "Necesita reparación",
            DictamenTecnico.NoRecuperable => "No recuperable",
            _ => "Funcional"
        };

        var pasos = new List<(string Titulo, string Icono, bool Hecho, DateTime? Fecha, string? Texto)>
        {
            ("Equipo recibido", "package", estado != EstadoEquipo.PendienteRecepcion, null, null),
            ("Diagnóstico técnico", "wrench", dictamen is not null, fechaDiagnostico,
                dictamen is null ? null : $"Dictamen: {dictamenTexto}.")
        };

        if (estado == EstadoEquipo.Baja)
        {
            pasos.Add(("Baja del equipo", "ban", true, fechaDiagnostico, "No recuperable: se destina a repuestos."));
        }
        else
        {
            pasos.Add(("Asignación a una solicitud", "swap", fechaAsignacion is not null, fechaAsignacion, null));
            pasos.Add(("Entrega completada", "truck", estado == EstadoEquipo.Entregado || fechaEntrega is not null, fechaEntrega, null));
        }

        var historial = new List<HitoViewModel> { registrada };
        var hayActual = false;

        foreach (var paso in pasos)
        {
            var estadoHito = paso.Hecho
                ? EstadoHito.Completado
                : hayActual ? EstadoHito.Pendiente : EstadoHito.Actual;

            hayActual |= !paso.Hecho;

            historial.Add(Hito(paso.Titulo, paso.Hecho ? paso.Fecha : null, paso.Icono, estadoHito, paso.Hecho ? paso.Texto : null));
        }

        return historial;
    }


    private static HitoViewModel Hito(string titulo, DateTime? fecha, string icono, EstadoHito estado, string? texto = null) => new()
    {
        Titulo = titulo,
        Fecha = fecha,
        Icono = icono,
        Estado = estado,
        Texto = texto
    };
}