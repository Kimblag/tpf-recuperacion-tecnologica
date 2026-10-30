using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
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

        consulta = FiltrarPorTexto(consulta, busqueda, incluirSolicitante: false);

        return await PaginarAsync(consulta, pagina, tamanoPagina);
    }

    public async Task<ResultadoPaginado<SolicitudFilaViewModel>> ListarTodasAsync(
        string? busqueda, TipoSolicitante? tipo, OrigenSolicitud? origen, EstadoSolicitud? estado,
        int pagina, int tamanoPagina)
    {
        var consulta = _context.Solicitudes.AsNoTracking();

        if (tipo is not null)
        {
            consulta = consulta.Where(s => s.TipoSolicitante == tipo);
        }

        if (origen is not null)
        {
            consulta = consulta.Where(s => s.OrigenSolicitud == origen);
        }

        if (estado is not null)
        {
            consulta = consulta.Where(s => s.EstadoSolicitud == estado);
        }

        consulta = FiltrarPorTexto(consulta, busqueda, incluirSolicitante: true);

        return await PaginarAsync(consulta, pagina, tamanoPagina);
    }

    private static IQueryable<Solicitud> FiltrarPorTexto(IQueryable<Solicitud> consulta, string? busqueda, bool incluirSolicitante)
    {
        var texto = busqueda?.Trim();

        if (string.IsNullOrEmpty(texto))
        {
            return consulta;
        }

        if (incluirSolicitante)
        {
            return consulta.Where(s =>
                ("SOL-" + (1000000 + s.NroSolicitud).ToString().Substring(1)).Contains(texto)
                || (1000000 + s.NroEquipoRequerido).ToString().Substring(1).Contains(texto)
                || (s.EquipoRequerido.Marca + " " + s.EquipoRequerido.Modelo).Contains(texto)
                || (s.Usuario.RazonSocial ?? (s.Usuario.Nombre + " " + s.Usuario.Apellido)).Contains(texto));
        }

        return consulta.Where(s =>
            ("SOL-" + (1000000 + s.NroSolicitud).ToString().Substring(1)).Contains(texto)
            || (1000000 + s.NroEquipoRequerido).ToString().Substring(1).Contains(texto)
            || (s.EquipoRequerido.Marca + " " + s.EquipoRequerido.Modelo).Contains(texto));
    }

    private async Task<ResultadoPaginado<SolicitudFilaViewModel>> PaginarAsync(
        IQueryable<Solicitud> consulta, int pagina, int tamanoPagina)
    {
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

    public Task<SolicitudDetalleViewModel?> ObtenerDetallePropioAsync(string identityUserId, int nroSolicitud) =>
        ObtenerDetalleAsync(
            _context.Solicitudes
                .AsNoTracking()
                .Where(s => s.NroSolicitud == nroSolicitud && s.Usuario.IdentityUserId == identityUserId),
            nroSolicitud);

    public Task<SolicitudDetalleViewModel?> ObtenerDetalleInternoAsync(int nroSolicitud) =>
        ObtenerDetalleAsync(
            _context.Solicitudes.AsNoTracking().Where(s => s.NroSolicitud == nroSolicitud),
            nroSolicitud);

    private async Task<SolicitudDetalleViewModel?> ObtenerDetalleAsync(IQueryable<Solicitud> consulta, int nroSolicitud)
    {
        var datos = await consulta
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
                s.Usuario.Apellido,
                s.Usuario.TipoUsuario,
                s.Usuario.CorreoElectronico
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
            TipoCuenta = datos.TipoUsuario,
            CorreoSolicitante = datos.CorreoElectronico,
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

        // nunca llegó a asignarse: se cerró porque el equipo se entregó a otra solicitud
        if (solicitud.Estado == EstadoSolicitud.Cerrada && asignacion is null)
        {
            hitos.Add(new HitoViewModel
            {
                Titulo = "Solicitud cerrada",
                Fecha = solicitud.FechaCierre,
                Icono = "clock",
                Estado = EstadoHito.Cancelado,
                Texto = "El equipo se entregó a otra solicitud."
            });

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