using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Entregas;

namespace TPF.RecuperacionTecnologica.Web.Services.Entregas;

public class EntregaService : IEntregaService
{
    private const int MaximoNombre = 100;

    private const string MensajeConcurrencia =
        "La asignación cambió mientras se registraba la entrega: otra persona pudo entregarla o liberarla. No se guardó nada. Volver a abrir la asignación e intentar nuevamente.";

    private readonly ApplicationDbContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<EntregaService> _logger;

    public EntregaService(
        ApplicationDbContext context,
        IAuditoriaService auditoriaService,
        ILogger<EntregaService> logger)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _logger = logger;
    }

    private static bool EsViolacionDeUnicidad(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };

    private static string NombreSolicitante(string? razonSocial, string? nombre, string? apellido) =>
        !string.IsNullOrWhiteSpace(razonSocial) ? razonSocial : $"{nombre} {apellido}".Trim();

    private static string SoloDigitos(string? documento) =>
        new((documento ?? string.Empty).Where(char.IsDigit).ToArray());

    public async Task<IReadOnlyList<AsignacionVigenteFilaViewModel>> BuscarAsignacionesVigentesAsync(
        string? busqueda, int cantidad)
    {
        var hoy = DateTime.UtcNow.Date;

        var filas = await _context.Asignaciones
            .AsNoTracking()
            .Where(a => a.EstadoAsignacion == EstadoAsignacion.Vigente)
            .OrderBy(a => a.FechaLimiteRetiro)
            .ThenBy(a => a.NroAsignacion)
            .Select(a => new
            {
                a.NroAsignacion,
                a.NroSolicitud,
                a.NroEquipo,
                a.Equipo.TipoEquipo,
                a.Equipo.Marca,
                a.Equipo.Modelo,
                a.FechaLimiteRetiro,
                a.Solicitud.Usuario.RazonSocial,
                a.Solicitud.Usuario.Nombre,
                a.Solicitud.Usuario.Apellido
            })
            .ToListAsync();

        // los códigos se arman en memoria: son pocas asignaciones vigentes
        var texto = busqueda?.Trim();

        return filas
            .Select(a => new AsignacionVigenteFilaViewModel
            {
                NroAsignacion = a.NroAsignacion,
                NroSolicitud = a.NroSolicitud,
                NroEquipo = a.NroEquipo,
                TipoEquipo = a.TipoEquipo,
                MarcaYModelo = $"{a.Marca} {a.Modelo}",
                Solicitante = NombreSolicitante(a.RazonSocial, a.Nombre, a.Apellido),
                FechaLimiteRetiro = a.FechaLimiteRetiro,
                PlazoVencido = a.FechaLimiteRetiro.Date < hoy
            })
            .Where(a => string.IsNullOrEmpty(texto)
                || a.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || $"SOL-{a.NroSolicitud:D6}".Contains(texto, StringComparison.OrdinalIgnoreCase)
                || a.CodigoEquipo.Contains(texto)
                || a.MarcaYModelo.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || a.Solicitante.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .Take(cantidad)
            .ToList();
    }

    public async Task<RegistroEntregaViewModel?> ObtenerParaRegistrarAsync(int nroAsignacion)
    {
        var datos = await _context.Asignaciones
            .AsNoTracking()
            .Where(a => a.NroAsignacion == nroAsignacion)
            .Select(a => new
            {
                a.EstadoAsignacion,
                a.NroSolicitud,
                a.Solicitud.TipoSolicitante,
                a.NroEquipo,
                a.Equipo.TipoEquipo,
                a.Equipo.Marca,
                a.Equipo.Modelo,
                a.FechaLimiteRetiro,
                a.Solicitud.Usuario.RazonSocial,
                a.Solicitud.Usuario.Nombre,
                a.Solicitud.Usuario.Apellido
            })
            .FirstOrDefaultAsync();

        if (datos is null)
        {
            return null;
        }

        return new RegistroEntregaViewModel
        {
            NroAsignacion = nroAsignacion,
            EstadoAsignacion = datos.EstadoAsignacion,
            NroSolicitud = datos.NroSolicitud,
            Solicitante = NombreSolicitante(datos.RazonSocial, datos.Nombre, datos.Apellido),
            TipoSolicitante = datos.TipoSolicitante,
            NroEquipo = datos.NroEquipo,
            TipoEquipo = datos.TipoEquipo,
            MarcaYModelo = $"{datos.Marca} {datos.Modelo}",
            FechaLimiteRetiro = datos.FechaLimiteRetiro,
            Situacion = datos.EstadoAsignacion != EstadoAsignacion.Vigente
                ? SituacionEntrega.NoVigente
                : datos.FechaLimiteRetiro.Date < DateTime.UtcNow.Date
                    ? SituacionEntrega.PlazoVencido
                    : SituacionEntrega.Lista
        };
    }

    public async Task<ResultadoEntrega> RegistrarAsync(
        string identityUserIdCoordinador, int nroAsignacion, string? nombreQuienRetira, string? documentoQuienRetira)
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
            return ResultadoEntrega.Fallo(ErrorEntrega.CoordinadorNoHabilitado,
                "La cuenta no está habilitada para registrar entregas.");
        }

        var nombre = nombreQuienRetira?.Trim();
        var documento = SoloDigitos(documentoQuienRetira);

        if (string.IsNullOrEmpty(nombre) || nombre.Length > MaximoNombre || documento.Length is < 7 or > 11)
        {
            return ResultadoEntrega.Fallo(ErrorEntrega.DatosInvalidos,
                "Los datos de quien retira no son válidos. Revisar el nombre y el documento.");
        }

        Entrega entrega;

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            // con seguimiento: el RowVersion de los tres detecta una liberación o una entrega concurrente
            var asignacion = await _context.Asignaciones
                .Include(a => a.Equipo)
                .Include(a => a.Solicitud).ThenInclude(s => s.Usuario)
                .FirstOrDefaultAsync(a => a.NroAsignacion == nroAsignacion);

            if (asignacion is null)
            {
                return ResultadoEntrega.Fallo(ErrorEntrega.AsignacionNoEncontrada, "No se encontró la asignación.");
            }

            if (asignacion.EstadoAsignacion != EstadoAsignacion.Vigente)
            {
                return ResultadoEntrega.Fallo(ErrorEntrega.AsignacionNoVigente,
                    "La asignación ya no está Vigente: se entregó o se liberó. Solo se puede entregar una asignación Vigente.");
            }

            var ahora = DateTime.UtcNow;

            if (asignacion.FechaLimiteRetiro.Date < ahora.Date)
            {
                return ResultadoEntrega.Fallo(ErrorEntrega.PlazoVencido,
                    "El plazo de retiro venció. Hay que liberar la asignación antes de volver a asignar el equipo.");
            }

            // se compara según el tipo de solicitante; el mensaje no revela el documento esperado
            if (asignacion.Solicitud.TipoSolicitante == TipoSolicitante.Particular
                && SoloDigitos(asignacion.Solicitud.Usuario.NroDocumento) != documento)
            {
                return ResultadoEntrega.Fallo(ErrorEntrega.DocumentoNoCoincide,
                    "El documento de quien retira no coincide con el de la persona titular de la solicitud.");
            }

            entrega = new Entrega
            {
                NroEquipo = asignacion.NroEquipo,
                NroSolicitud = asignacion.NroSolicitud,
                NroAsignacion = asignacion.NroAsignacion,
                CodigoCoordinador = codigoCoordinador.Value,
                FechaEntrega = ahora,
                NombreQuienRetira = nombre,
                DocumentoIdentidadQuienRetira = documento
            };

            _context.Entregas.Add(entrega);

            asignacion.EstadoAsignacion = EstadoAsignacion.Concretada;
            asignacion.FechaUltimaModificacion = ahora;

            asignacion.Equipo.EstadoEquipo = EstadoEquipo.Entregado;
            asignacion.Equipo.FechaUltimaModificacion = ahora;

            asignacion.Solicitud.EstadoSolicitud = EstadoSolicitud.Completada;
            asignacion.Solicitud.FechaCierre = ahora;
            asignacion.Solicitud.FechaUltimaModificacion = ahora;

            // las demás solicitudes en espera del equipo ya no pueden atenderse: si quedaran abiertas
            // seguirían ocupando el cupo de pendientes de sus titulares
            var enEspera = await _context.Solicitudes
                .Where(s => s.NroEquipoRequerido == asignacion.NroEquipo
                    && s.NroSolicitud != asignacion.NroSolicitud
                    && s.EstadoSolicitud == EstadoSolicitud.Pendiente)
                .ToListAsync();

            foreach (var otra in enEspera)
            {
                otra.EstadoSolicitud = EstadoSolicitud.Cerrada;
                otra.FechaCierre = ahora;
                otra.FechaUltimaModificacion = ahora;
            }

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoEntrega.Fallo(ErrorEntrega.EntregaConcurrente, MensajeConcurrencia);
        }
        catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
        {
            return ResultadoEntrega.Fallo(ErrorEntrega.EntregaConcurrente, MensajeConcurrencia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la entrega de la Asignación {NroAsignacion}", nroAsignacion);
            return ResultadoEntrega.Fallo(ErrorEntrega.Fallido,
                "No se pudo registrar la entrega. No se guardó nada. Intentar nuevamente.");
        }

        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.Entrega, codigoCoordinador, nameof(Entrega),
                entrega.NroEntrega, $"Entrega del equipo {entrega.NroEquipo} de la asignación {nroAsignacion}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar la Entrega {NroEntrega}", entrega.NroEntrega);
        }

        return ResultadoEntrega.Ok(entrega.NroEntrega);
    }
}
