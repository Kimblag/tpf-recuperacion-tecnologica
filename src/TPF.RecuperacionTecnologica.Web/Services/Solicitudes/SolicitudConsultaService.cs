using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Solicitudes;

namespace TPF.RecuperacionTecnologica.Web.Services.Solicitudes;

public class SolicitudConsultaService : ISolicitudConsultaService
{
    private sealed record DatosSolicitud(
        int NroSolicitud, OrigenSolicitud Origen, DateTime FechaSolicitud, EstadoSolicitud Estado,
        DateTime? FechaCancelacion, string? MotivoCancelacion, DateTime? FechaCierre);

    private sealed record DatosAsignacion(
        int NroSolicitud, DateTime FechaAsignacion, DateTime FechaLimiteRetiro,
        DateTime? FechaLiberacion, string? MotivoLiberacion);

    private readonly ApplicationDbContext _context;

    public SolicitudConsultaService(ApplicationDbContext context)
    {
        _context = context;
    }

    private static string NombreSolicitante(string? razonSocial, string? nombre, string? apellido) =>
        !string.IsNullOrWhiteSpace(razonSocial) ? razonSocial : $"{nombre} {apellido}".Trim();

    public async Task<ResultadoPaginado<SolicitudFilaViewModel>> ListarPropiasAsync(
        string identityUserId, string? busqueda, EstadoSolicitud? estado, int pagina, int tamanoPagina)
    {
        var consulta = _context.Solicitudes
            .AsNoTracking()
            .Where(s => s.Usuario.IdentityUserId == identityUserId);

        if (estado is not null)
        {
            consulta = consulta.Where(s => s.EstadoSolicitud == estado);
        }

        var texto = busqueda?.Trim();

        if (!string.IsNullOrEmpty(texto))
        {
            consulta = consulta.Where(s =>
                ("SOL-" + (1000000 + s.NroSolicitud).ToString().Substring(1)).Contains(texto)
                || (1000000 + s.NroEquipoRequerido).ToString().Substring(1).Contains(texto)
                || (s.EquipoRequerido.Marca + " " + s.EquipoRequerido.Modelo).Contains(texto));
        }

        var total = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var filas = await consulta
            .OrderByDescending(s => s.FechaSolicitud)
            .ThenByDescending(s => s.NroSolicitud)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(s => new
            {
                s.NroSolicitud,
                s.NroEquipoRequerido,
                s.EquipoRequerido.TipoEquipo,
                s.EquipoRequerido.Marca,
                s.EquipoRequerido.Modelo,
                s.TipoSolicitante,
                s.OrigenSolicitud,
                s.FechaSolicitud,
                s.EstadoSolicitud,
                s.Usuario.RazonSocial,
                s.Usuario.Nombre,
                s.Usuario.Apellido
            })
            .ToListAsync();

        return new ResultadoPaginado<SolicitudFilaViewModel>
        {
            Elementos = filas.Select(f => new SolicitudFilaViewModel
            {
                NroSolicitud = f.NroSolicitud,
                NroEquipo = f.NroEquipoRequerido,
                TipoEquipo = f.TipoEquipo,
                MarcaYModelo = $"{f.Marca} {f.Modelo}",
                Solicitante = NombreSolicitante(f.RazonSocial, f.Nombre, f.Apellido),
                TipoSolicitante = f.TipoSolicitante,
                OrigenSolicitud = f.OrigenSolicitud,
                FechaSolicitud = f.FechaSolicitud,
                EstadoSolicitud = f.EstadoSolicitud
            }).ToList(),
            Total = total,
            Pagina = pagina
        };
    }

    public async Task<SolicitudDetalleViewModel?> ObtenerDetallePropioAsync(string identityUserId, int nroSolicitud)
    {
        var datos = await _context.Solicitudes
            .AsNoTracking()
            .Where(s => s.NroSolicitud == nroSolicitud && s.Usuario.IdentityUserId == identityUserId)
            .Select(s => new
            {
                s.NroEquipoRequerido,
                s.EquipoRequerido.TipoEquipo,
                s.EquipoRequerido.Marca,
                s.EquipoRequerido.Modelo,
                s.TipoSolicitante,
                s.MotivoSolicitud,
                s.OrigenSolicitud,
                s.FechaSolicitud,
                s.EstadoSolicitud,
                s.FechaCancelacion,
                s.MotivoCancelacion,
                s.FechaCierre,
                s.Usuario.RazonSocial,
                s.Usuario.Nombre,
                s.Usuario.Apellido
            })
            .FirstOrDefaultAsync();

        if (datos is null)
        {
            return null;
        }

        var asignacion = await _context.Asignaciones
            .AsNoTracking()
            .Where(a => a.NroSolicitud == nroSolicitud)
            .OrderByDescending(a => a.FechaAsignacion)
            .Select(a => new DatosAsignacion(a.NroSolicitud, a.FechaAsignacion, a.FechaLimiteRetiro, a.FechaLiberacion, a.MotivoLiberacion))
            .FirstOrDefaultAsync();

        var fechaEntrega = await _context.Entregas
            .AsNoTracking()
            .Where(e => e.NroSolicitud == nroSolicitud)
            .Select(e => (DateTime?)e.FechaEntrega)
            .FirstOrDefaultAsync();

        var solicitud = new DatosSolicitud(
            nroSolicitud, datos.OrigenSolicitud, datos.FechaSolicitud, datos.EstadoSolicitud,
            datos.FechaCancelacion, datos.MotivoCancelacion, datos.FechaCierre);

        return new SolicitudDetalleViewModel
        {
            NroSolicitud = nroSolicitud,
            NroEquipo = datos.NroEquipoRequerido,
            TipoEquipo = datos.TipoEquipo,
            MarcaYModelo = $"{datos.Marca} {datos.Modelo}",
            Solicitante = NombreSolicitante(datos.RazonSocial, datos.Nombre, datos.Apellido),
            TipoSolicitante = datos.TipoSolicitante,
            MotivoSolicitud = datos.MotivoSolicitud,
            OrigenSolicitud = datos.OrigenSolicitud,
            FechaSolicitud = datos.FechaSolicitud,
            EstadoSolicitud = datos.EstadoSolicitud,
            Hitos = ArmarHitos(solicitud, asignacion, fechaEntrega)
        };
    }

    private static List<HitoViewModel> ArmarHitos(DatosSolicitud solicitud, DatosAsignacion? asignacion, DateTime? fechaEntrega)
    {
        var hitos = new List<HitoViewModel>
        {
            new()
            {
                Titulo = "Solicitud registrada",
                Fecha = solicitud.FechaSolicitud,
                Icono = "tray",
                Estado = EstadoHito.Completado,
                Texto = solicitud.Origen == OrigenSolicitud.Asistida
                    ? "Registrada con ayuda de una persona coordinadora."
                    : "Registrada por la propia persona."
            }
        };

        if (solicitud.Estado == EstadoSolicitud.Cancelada)
        {
            hitos.Add(new HitoViewModel
            {
                Titulo = "Solicitud cancelada",
                Fecha = solicitud.FechaCancelacion,
                Icono = "x",
                Estado = EstadoHito.Cancelado,
                Texto = "Cancelada por la persona solicitante."
            });

            return hitos;
        }

        if (solicitud.Estado == EstadoSolicitud.Pendiente)
        {
            hitos.Add(new HitoViewModel
            {
                Titulo = "Asignación de equipo",
                Icono = "swap",
                Estado = EstadoHito.Actual,
                Texto = "En espera de una persona coordinadora."
            });
            hitos.Add(new HitoViewModel { Titulo = "Entrega del equipo", Icono = "truck", Estado = EstadoHito.Pendiente });

            return hitos;
        }

        hitos.Add(new HitoViewModel
        {
            Titulo = "Equipo asignado",
            Fecha = asignacion?.FechaAsignacion,
            Icono = "swap",
            Estado = EstadoHito.Completado,
            Texto = solicitud.Estado == EstadoSolicitud.Asignada && asignacion is not null
                ? $"Hay que retirarlo hasta el {asignacion.FechaLimiteRetiro:dd/MM/yyyy}."
                : null
        });

        switch (solicitud.Estado)
        {
            case EstadoSolicitud.Asignada:
                hitos.Add(new HitoViewModel { Titulo = "Entrega del equipo", Icono = "truck", Estado = EstadoHito.Pendiente });
                break;

            case EstadoSolicitud.Completada:
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Entrega completada",
                    Fecha = fechaEntrega,
                    Icono = "truck",
                    Estado = EstadoHito.Completado
                });
                break;

            default:
                hitos.Add(new HitoViewModel
                {
                    Titulo = "Solicitud cerrada",
                    Fecha = solicitud.FechaCierre ?? asignacion?.FechaLiberacion,
                    Icono = "clock",
                    Estado = EstadoHito.Cancelado,
                    Texto = string.IsNullOrWhiteSpace(asignacion?.MotivoLiberacion)
                        ? "La asignación fue liberada."
                        : $"La asignación fue liberada. Motivo: {asignacion.MotivoLiberacion}"
                });
                break;
        }

        return hitos;
    }

    public async Task<EditarMotivoSolicitudViewModel?> ObtenerParaEdicionPropiaAsync(string identityUserId, int nroSolicitud)
    {
        var datos = await _context.Solicitudes
            .AsNoTracking()
            .Where(s => s.NroSolicitud == nroSolicitud && s.Usuario.IdentityUserId == identityUserId)
            .Select(s => new
            {
                s.NroEquipoRequerido,
                s.EquipoRequerido.TipoEquipo,
                s.EquipoRequerido.Marca,
                s.EquipoRequerido.Modelo,
                s.TipoSolicitante,
                s.MotivoSolicitud,
                s.OrigenSolicitud,
                s.FechaSolicitud,
                s.EstadoSolicitud,
                s.Usuario.RazonSocial,
                s.Usuario.Nombre,
                s.Usuario.Apellido
            })
            .FirstOrDefaultAsync();

        if (datos is null)
        {
            return null;
        }

        return new EditarMotivoSolicitudViewModel
        {
            NroSolicitud = nroSolicitud,
            MotivoSolicitud = datos.MotivoSolicitud,
            NroEquipo = datos.NroEquipoRequerido,
            TipoEquipo = datos.TipoEquipo,
            MarcaYModelo = $"{datos.Marca} {datos.Modelo}",
            Solicitante = NombreSolicitante(datos.RazonSocial, datos.Nombre, datos.Apellido),
            TipoSolicitante = datos.TipoSolicitante,
            OrigenSolicitud = datos.OrigenSolicitud,
            FechaSolicitud = datos.FechaSolicitud,
            EstadoSolicitud = datos.EstadoSolicitud
        };
    }
}