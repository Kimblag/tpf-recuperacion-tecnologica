using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.Services.Cuenta;
using TPF.RecuperacionTecnologica.Web.Services.Usuarios;
using TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

namespace TPF.RecuperacionTecnologica.Web.Services.Donaciones;

public class DonacionService : IDonacionService
{
    private readonly ApplicationDbContext _context;
    private readonly IUsuarioService _usuarioService;
    private readonly ICuentaService _cuentaService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<DonacionService> _logger;

    public DonacionService(
        ApplicationDbContext context,
        IUsuarioService usuarioService,
        ICuentaService cuentaService,
        IAuditoriaService auditoriaService,
        ILogger<DonacionService> logger)
    {
        _context = context;
        _usuarioService = usuarioService;
        _cuentaService = cuentaService;
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

    private static Equipo NuevoEquipo(int nroUsuarioDonante, RegistroDonacionViewModel modelo) => new()
    {
        NroUsuarioDonante = nroUsuarioDonante,
        TipoEquipo = modelo.TipoEquipo!.Value,
        Marca = modelo.Marca.Trim(),
        Modelo = modelo.Modelo.Trim(),
        EstadoInicialDeclarado = modelo.EstadoInicialDeclarado!.Value,
        Observaciones = TextoOpcional(modelo.Observaciones),
        EspecificacionesDeclaradas = EspecificacionesDelTipo(modelo.TipoEquipo!.Value, modelo),
        EstadoEquipo = EstadoEquipo.PendienteRecepcion,
        FechaRegistro = DateTime.UtcNow
    };

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

        var equipo = NuevoEquipo(nroUsuario.Value, modelo);

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

    private static bool EsViolacionDeUnicidad(DbUpdateException ex) =>
        ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 };

    private async Task AuditarAsistidaAsync(
        TipoOperacionAuditoria tipo, int codigoPersonal, string entidad, int id, string detalle)
    {
        try
        {
            await _auditoriaService.RegistrarEventoAsync(tipo, codigoPersonal, entidad, id, detalle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar el evento {Tipo} de la entidad {Entidad} {Id}", tipo, entidad, id);
        }
    }

    public async Task<ResultadoRegistroDonacion> RegistrarAsistidaAsync(
        string identityUserIdCoordinador, RegistroDonacionAsistidaViewModel modelo,
        Func<string, string, string> construirRutaInvitacion, string urlBaseSolicitud)
    {
        var codigoCoordinador = await _context.PersonalInterno
            .AsNoTracking()
            .Where(p => p.IdentityUserId == identityUserIdCoordinador
                && p.RolAsignado == RolPersonal.Coordinador
                && p.EstadoPersonal == EstadoPersonal.Activo)
            .Select(p => (int?)p.CodigoPersonal)
            .FirstOrDefaultAsync();

        if (codigoCoordinador is null)
        {
            return ResultadoRegistroDonacion.Fallo(ErrorRegistroDonacion.CoordinadorNoHabilitado,
                "La cuenta no está habilitada para registrar donaciones asistidas.");
        }

        return modelo.Usuario.Modo == ModoUsuarioAsistido.Existente
            ? await RegistrarAsistidaParaExistenteAsync(codigoCoordinador.Value, modelo)
            : await RegistrarAsistidaParaNuevoAsync(codigoCoordinador.Value, modelo, construirRutaInvitacion, urlBaseSolicitud);
    }

    private async Task<ResultadoRegistroDonacion> RegistrarAsistidaParaExistenteAsync(
        int codigoCoordinador, RegistroDonacionAsistidaViewModel modelo)
    {
        var donante = await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.NroUsuario == modelo.Usuario.NroUsuario)
            .Select(u => new { u.NroUsuario, u.EstadoUsuario })
            .FirstOrDefaultAsync();

        if (donante is null || donante.EstadoUsuario != EstadoUsuario.Activo)
        {
            return ResultadoRegistroDonacion.Fallo(ErrorRegistroDonacion.UsuarioNoHabilitado,
                "El usuario no existe o no está activo, por eso no se puede registrar una donación a su nombre.");
        }

        var equipo = NuevoEquipo(donante.NroUsuario, modelo);
        _context.Equipos.Add(equipo);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el registro asistido de la donación del Usuario {NroUsuario}", donante.NroUsuario);
            return ResultadoRegistroDonacion.Fallo(ErrorRegistroDonacion.Fallido,
                "No se pudo registrar la donación. Intentar nuevamente.");
        }

        await AuditarAsistidaAsync(TipoOperacionAuditoria.Donacion, codigoCoordinador, nameof(Equipo),
            equipo.NroEquipo, $"Donación asistida de equipo ({equipo.TipoEquipo})");

        return ResultadoRegistroDonacion.Ok(equipo.NroEquipo);
    }

    private async Task<ResultadoRegistroDonacion> RegistrarAsistidaParaNuevoAsync(
        int codigoCoordinador, RegistroDonacionAsistidaViewModel modelo,
        Func<string, string, string> construirRutaInvitacion, string urlBaseSolicitud)
    {
        string identityUserId;
        Usuario usuario;
        Equipo equipo;

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            var alta = await _usuarioService.AltaAsistidaAsync(modelo.Usuario);

            if (!alta.Exitoso)
            {
                return alta.YaExiste
                    ? ResultadoRegistroDonacion.Fallo(ErrorRegistroDonacion.UsuarioYaExiste, alta.Mensaje!)
                    : ResultadoRegistroDonacion.Fallo(ErrorRegistroDonacion.Fallido, alta.Mensaje ?? "No se pudo crear la cuenta de la persona.");
            }

            usuario = alta.Usuario!;
            identityUserId = alta.IdentityUserId!;

            equipo = NuevoEquipo(usuario.NroUsuario, modelo);
            _context.Equipos.Add(equipo);
            await _context.SaveChangesAsync();

            await transaccion.CommitAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
        {
            return ResultadoRegistroDonacion.Fallo(ErrorRegistroDonacion.UsuarioYaExiste, ResultadoAltaAsistida.MensajeYaExiste);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el alta asistida del donante y su donación; se revirtió la transacción");
            return ResultadoRegistroDonacion.Fallo(ErrorRegistroDonacion.Fallido,
                "No se pudo completar el registro. No se guardó nada. Intentar nuevamente.");
        }

        await AuditarAsistidaAsync(TipoOperacionAuditoria.AltaUsuario, codigoCoordinador, nameof(Usuario),
            usuario.NroUsuario, $"Alta asistida de usuario ({usuario.TipoUsuario})");
        await AuditarAsistidaAsync(TipoOperacionAuditoria.Donacion, codigoCoordinador, nameof(Equipo),
            equipo.NroEquipo, $"Donación asistida de equipo ({equipo.TipoEquipo})");

        try
        {
            await _cuentaService.EnviarInvitacionAsync(identityUserId, construirRutaInvitacion, urlBaseSolicitud);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar la invitación al Usuario {NroUsuario}", usuario.NroUsuario);
        }

        return ResultadoRegistroDonacion.Ok(equipo.NroEquipo);
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

    public async Task<ResultadoPaginado<DonacionGestionFilaViewModel>> ListarTodasAsync(
        string? busqueda, string? donante, EstadoEquipo? estado, int pagina, int tamanoPagina)
    {
        var consulta = _context.Equipos.AsNoTracking();

        if (estado is not null)
        {
            consulta = consulta.Where(e => e.EstadoEquipo == estado);
        }

        var texto = busqueda?.Trim();

        if (!string.IsNullOrEmpty(texto))
        {
            consulta = int.TryParse(texto, out var nroEquipo)
                ? consulta.Where(e => e.NroEquipo == nroEquipo)
                : consulta.Where(e => e.Marca.Contains(texto) || e.Modelo.Contains(texto));
        }

        var textoDonante = donante?.Trim();

        if (!string.IsNullOrEmpty(textoDonante))
        {
            consulta = consulta.Where(e =>
                (e.UsuarioDonante.Nombre + " " + e.UsuarioDonante.Apellido).Contains(textoDonante)
                || e.UsuarioDonante.RazonSocial!.Contains(textoDonante)
                || e.UsuarioDonante.CorreoElectronico.Contains(textoDonante));
        }

        var total = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var elementos = await consulta
            .OrderByDescending(e => e.FechaRegistro)
            .ThenByDescending(e => e.NroEquipo)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(e => new DonacionGestionFilaViewModel
            {
                NroEquipo = e.NroEquipo,
                TipoEquipo = e.TipoEquipo,
                Marca = e.Marca,
                Modelo = e.Modelo,
                EstadoInicialDeclarado = e.EstadoInicialDeclarado,
                FechaRegistro = e.FechaRegistro,
                EstadoEquipo = e.EstadoEquipo,
                Donante = (e.UsuarioDonante.TipoUsuario == TipoUsuario.Persona
                    ? e.UsuarioDonante.Nombre + " " + e.UsuarioDonante.Apellido
                    : e.UsuarioDonante.RazonSocial) ?? string.Empty
            })
            .ToListAsync();

        return new ResultadoPaginado<DonacionGestionFilaViewModel>
        {
            Elementos = elementos,
            Total = total,
            Pagina = pagina
        };
    }

    public Task<DonacionDetalleViewModel?> ObtenerDetalleAsync(string identityUserId, int nroEquipo) =>
        ArmarDetalleAsync(_context.Equipos.Where(e => e.UsuarioDonante.IdentityUserId == identityUserId), nroEquipo);

    public Task<DonacionDetalleViewModel?> ObtenerDetalleInternoAsync(int nroEquipo) =>
        ArmarDetalleAsync(_context.Equipos, nroEquipo);

    // la consulta de origen define qué donaciones se pueden ver: las propias del Usuario o todas
    private async Task<DonacionDetalleViewModel?> ArmarDetalleAsync(IQueryable<Equipo> origen, int nroEquipo)
    {
        var equipo = await origen
            .AsNoTracking()
            .Where(e => e.NroEquipo == nroEquipo)
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


    public async Task<ResultadoCatalogo> ObtenerDisponiblesAsync(
    string? busqueda, string? marca, TipoEquipo? tipo, OrdenCatalogo orden, int pagina, int tamanoPagina)
    {
        var disponibles = _context.Equipos
            .AsNoTracking()
            .Where(e => e.EstadoEquipo == EstadoEquipo.Disponible)
            .Join(_context.Diagnosticos.Where(d => d.EsVigente),
                e => e.NroEquipo, d => d.NroEquipo,
                (e, d) => new { Equipo = e, Diagnostico = d });

        var totalDisponibles = await disponibles.CountAsync();

        var marcas = await disponibles
            .Select(x => x.Equipo.Marca)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();

        var consulta = disponibles;

        if (tipo is not null)
        {
            consulta = consulta.Where(x => x.Equipo.TipoEquipo == tipo);
        }

        if (!string.IsNullOrWhiteSpace(marca))
        {
            consulta = consulta.Where(x => x.Equipo.Marca == marca);
        }

        var texto = busqueda?.Trim();

        if (!string.IsNullOrEmpty(texto))
        {
            var esNumero = int.TryParse(texto, out var nroEquipo);

            consulta = consulta.Where(x =>
                (esNumero && x.Equipo.NroEquipo == nroEquipo)
                || x.Equipo.Marca.Contains(texto)
                || x.Equipo.Modelo.Contains(texto)
                || (x.Diagnostico.EspecificacionesVerificadas.Procesador != null && x.Diagnostico.EspecificacionesVerificadas.Procesador.Contains(texto))
                || (x.Diagnostico.EspecificacionesVerificadas.MemoriaRam != null && x.Diagnostico.EspecificacionesVerificadas.MemoriaRam.Contains(texto))
                || (x.Diagnostico.EspecificacionesVerificadas.Almacenamiento != null && x.Diagnostico.EspecificacionesVerificadas.Almacenamiento.Contains(texto))
                || (x.Diagnostico.EspecificacionesVerificadas.TamanoPantalla != null && x.Diagnostico.EspecificacionesVerificadas.TamanoPantalla.Contains(texto))
                || (x.Diagnostico.EspecificacionesVerificadas.EstadoBateria != null && x.Diagnostico.EspecificacionesVerificadas.EstadoBateria.Contains(texto))
                || (x.Diagnostico.EspecificacionesVerificadas.Otros != null && x.Diagnostico.EspecificacionesVerificadas.Otros.Contains(texto)));
        }

        var total = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var ordenada = orden switch
        {
            OrdenCatalogo.Antiguos => consulta.OrderBy(x => x.Equipo.FechaDisponibilidad),
            OrdenCatalogo.MarcaAsc => consulta.OrderBy(x => x.Equipo.Marca).ThenBy(x => x.Equipo.Modelo),
            OrdenCatalogo.MarcaDesc => consulta.OrderByDescending(x => x.Equipo.Marca).ThenByDescending(x => x.Equipo.Modelo),
            _ => consulta.OrderByDescending(x => x.Equipo.FechaDisponibilidad)
        };

        var elementos = await ordenada
            .ThenBy(x => x.Equipo.NroEquipo)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(x => new EquipoDisponibleViewModel
            {
                NroEquipo = x.Equipo.NroEquipo,
                TipoEquipo = x.Equipo.TipoEquipo,
                Marca = x.Equipo.Marca,
                Modelo = x.Equipo.Modelo,
                FechaDisponibilidad = x.Equipo.FechaDisponibilidad,
                Procesador = x.Diagnostico.EspecificacionesVerificadas.Procesador,
                MemoriaRam = x.Diagnostico.EspecificacionesVerificadas.MemoriaRam,
                Almacenamiento = x.Diagnostico.EspecificacionesVerificadas.Almacenamiento,
                TamanoPantalla = x.Diagnostico.EspecificacionesVerificadas.TamanoPantalla,
                EstadoBateria = x.Diagnostico.EspecificacionesVerificadas.EstadoBateria,
                Otros = x.Diagnostico.EspecificacionesVerificadas.Otros,
                ImagenUrl = _context.ImagenesEquipo
                    .Where(i => i.NroEquipo == x.Equipo.NroEquipo)
                    .OrderBy(i => i.FechaCarga)
                    .Select(i => i.UrlImagen)
                    .FirstOrDefault(),
                CantidadImagenes = _context.ImagenesEquipo.Count(i => i.NroEquipo == x.Equipo.NroEquipo),
                SolicitudesEnEspera = _context.Solicitudes.Count(s =>
                    s.NroEquipoRequerido == x.Equipo.NroEquipo && s.EstadoSolicitud == EstadoSolicitud.Pendiente)
            })
            .ToListAsync();

        return new ResultadoCatalogo
        {
            Elementos = elementos,
            Total = total,
            Pagina = pagina,
            TotalDisponibles = totalDisponibles,
            Marcas = marcas
        };
    }

    public async Task<EquipoCatalogoDetalleViewModel?> ObtenerDetalleDisponibleAsync(int nroEquipo)
    {
        // un equipo que no está Disponible responde igual que uno inexistente
        var detalle = await _context.Equipos
            .AsNoTracking()
            .Where(e => e.NroEquipo == nroEquipo && e.EstadoEquipo == EstadoEquipo.Disponible)
            .Join(_context.Diagnosticos.Where(d => d.EsVigente),
                e => e.NroEquipo, d => d.NroEquipo,
                (e, d) => new { Equipo = e, Diagnostico = d })
            .Select(x => new EquipoCatalogoDetalleViewModel
            {
                NroEquipo = x.Equipo.NroEquipo,
                TipoEquipo = x.Equipo.TipoEquipo,
                Marca = x.Equipo.Marca,
                Modelo = x.Equipo.Modelo,
                FechaDisponibilidad = x.Equipo.FechaDisponibilidad,
                FechaRevision = x.Diagnostico.FechaDiagnostico,
                DictamenTecnico = x.Diagnostico.DictamenTecnico,
                SolicitudesEnEspera = _context.Solicitudes.Count(s =>
                    s.NroEquipoRequerido == x.Equipo.NroEquipo && s.EstadoSolicitud == EstadoSolicitud.Pendiente),
                Procesador = x.Diagnostico.EspecificacionesVerificadas.Procesador,
                MemoriaRam = x.Diagnostico.EspecificacionesVerificadas.MemoriaRam,
                Almacenamiento = x.Diagnostico.EspecificacionesVerificadas.Almacenamiento,
                TamanoPantalla = x.Diagnostico.EspecificacionesVerificadas.TamanoPantalla,
                EstadoBateria = x.Diagnostico.EspecificacionesVerificadas.EstadoBateria,
                Otros = x.Diagnostico.EspecificacionesVerificadas.Otros
            })
            .FirstOrDefaultAsync();

        if (detalle is null)
        {
            return null;
        }

        var imagenes = await _context.ImagenesEquipo
            .AsNoTracking()
            .Where(i => i.NroEquipo == nroEquipo)
            .OrderBy(i => i.FechaCarga)
            .Select(i => new { i.UrlImagen, i.Angulo })
            .ToListAsync();

        detalle.Imagenes = imagenes.Select(i => (i.UrlImagen, i.Angulo)).ToList();
        detalle.ImagenUrl = imagenes.FirstOrDefault()?.UrlImagen;

        return detalle;
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