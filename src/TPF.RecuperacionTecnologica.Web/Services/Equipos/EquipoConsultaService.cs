using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.Services.Equipos;

public class EquipoConsultaService : IEquipoConsultaService
{
    private readonly ApplicationDbContext _context;

    public EquipoConsultaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ResultadoPaginado<EquipoFilaViewModel>> ListarAsync(
        string? busqueda, EstadoEquipo? estado, TipoEquipo? tipo, bool soloPendientesDeDiagnostico,
        OrdenEquipo orden, int pagina, int tamanoPagina)
    {
        var consulta = _context.Equipos.AsNoTracking();

        if (soloPendientesDeDiagnostico)
        {
            consulta = consulta.Where(e => e.EstadoEquipo == EstadoEquipo.PendienteRecepcion || e.EstadoEquipo == EstadoEquipo.EnReparacion);

            if (estado is not (EstadoEquipo.PendienteRecepcion or EstadoEquipo.EnReparacion))
            {
                estado = null;
            }
        }

        if (estado is not null)
        {
            consulta = consulta.Where(e => e.EstadoEquipo == estado);
        }

        if (tipo is not null)
        {
            consulta = consulta.Where(e => e.TipoEquipo == tipo);
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
            OrdenEquipo.RegistroDesc => consulta.OrderByDescending(e => e.FechaRegistro),
            OrdenEquipo.EquipoAsc => consulta.OrderBy(e => e.NroEquipo),
            OrdenEquipo.EquipoDesc => consulta.OrderByDescending(e => e.NroEquipo),
            OrdenEquipo.TipoAsc => consulta.OrderBy(rangoTipo),
            OrdenEquipo.TipoDesc => consulta.OrderByDescending(rangoTipo),
            OrdenEquipo.ModeloAsc => consulta.OrderBy(e => e.Marca).ThenBy(e => e.Modelo),
            OrdenEquipo.ModeloDesc => consulta.OrderByDescending(e => e.Marca).ThenByDescending(e => e.Modelo),
            OrdenEquipo.CondicionAsc => consulta.OrderBy(rangoCondicion),
            OrdenEquipo.CondicionDesc => consulta.OrderByDescending(rangoCondicion),
            OrdenEquipo.EstadoAsc => consulta.OrderBy(e => e.EstadoEquipo),
            OrdenEquipo.EstadoDesc => consulta.OrderByDescending(e => e.EstadoEquipo),
            _ => consulta.OrderBy(e => e.FechaRegistro)
        };

        var elementos = await ordenada
            .ThenBy(e => e.NroEquipo)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(e => new EquipoFilaViewModel
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

        return new ResultadoPaginado<EquipoFilaViewModel>
        {
            Elementos = elementos,
            Total = total,
            Pagina = pagina
        };
    }

    public async Task<EquipoDetalleTecnicoViewModel?> ObtenerDetalleAsync(int nroEquipo)
    {
        var datos = await _context.Equipos
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
                e.FechaDisponibilidad,
                e.FechaCancelacion,
                e.MotivoCancelacion,
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

        var vigente = await _context.Diagnosticos
            .AsNoTracking()
            .Where(d => d.NroEquipo == nroEquipo && d.EsVigente)
            .Select(d => new DiagnosticoVigenteViewModel
            {
                NroDiagnostico = d.NroDiagnostico,
                TipoDiagnostico = d.TipoDiagnostico,
                FechaDiagnostico = d.FechaDiagnostico,
                Tecnico = d.Tecnico.Nombre + " " + d.Tecnico.Apellido,
                DictamenTecnico = d.DictamenTecnico,
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

        var donante = datos.TipoUsuario == TipoUsuario.Persona
            ? $"{datos.Nombre} {datos.Apellido}".Trim()
            : datos.RazonSocial ?? string.Empty;

        return new EquipoDetalleTecnicoViewModel
        {
            NroEquipo = datos.NroEquipo,
            TipoEquipo = datos.TipoEquipo,
            Marca = datos.Marca,
            Modelo = datos.Modelo,
            EstadoInicialDeclarado = datos.EstadoInicialDeclarado,
            Observaciones = datos.Observaciones,
            EstadoEquipo = datos.EstadoEquipo,
            FechaRegistro = datos.FechaRegistro,
            FechaDisponibilidad = datos.FechaDisponibilidad,
            FechaCancelacion = datos.FechaCancelacion,
            MotivoCancelacion = datos.MotivoCancelacion,
            Donante = donante,
            Procesador = datos.Procesador,
            MemoriaRam = datos.MemoriaRam,
            Almacenamiento = datos.Almacenamiento,
            TamanoPantalla = datos.TamanoPantalla,
            EstadoBateria = datos.EstadoBateria,
            Otros = datos.Otros,
            DiagnosticoVigente = vigente
        };
    }

    public async Task<HistorialDiagnosticosViewModel?> ObtenerHistorialAsync(int nroEquipo)
    {
        var equipo = await _context.Equipos
            .AsNoTracking()
            .Where(e => e.NroEquipo == nroEquipo)
            .Select(e => new { e.NroEquipo, e.TipoEquipo, e.Marca, e.Modelo, e.EstadoEquipo })
            .FirstOrDefaultAsync();

        if (equipo is null)
        {
            return null;
        }

        var diagnosticos = await _context.Diagnosticos
            .AsNoTracking()
            .Where(d => d.NroEquipo == nroEquipo)
            .OrderByDescending(d => d.FechaDiagnostico)
            .ThenByDescending(d => d.NroDiagnostico)
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

        var vigente = await _context.Diagnosticos
            .AsNoTracking()
            .Where(d => d.NroEquipo == nroEquipo && d.EsVigente)
            .Select(d => new DiagnosticoVigenteViewModel
            {
                NroDiagnostico = d.NroDiagnostico,
                TipoDiagnostico = d.TipoDiagnostico,
                FechaDiagnostico = d.FechaDiagnostico,
                Tecnico = d.Tecnico.Nombre + " " + d.Tecnico.Apellido,
                DictamenTecnico = d.DictamenTecnico,
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

        return new HistorialDiagnosticosViewModel
        {
            NroEquipo = equipo.NroEquipo,
            TipoEquipo = equipo.TipoEquipo,
            Marca = equipo.Marca,
            Modelo = equipo.Modelo,
            EstadoEquipo = equipo.EstadoEquipo,
            Vigente = vigente,
            Diagnosticos = diagnosticos
        };
    }

    public async Task<LegajoEquipoViewModel?> ObtenerLegajoAsync(int nroEquipo)
    {
        var equipo = await _context.Equipos
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
                e.FechaDisponibilidad,
                e.FechaCancelacion,
                e.MotivoCancelacion,
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

        var diagnosticos = await _context.Diagnosticos
            .AsNoTracking()
            .Where(d => d.NroEquipo == nroEquipo)
            .OrderBy(d => d.FechaDiagnostico)
            .ThenBy(d => d.NroDiagnostico)
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

        // consulta de solicitudes por equipo; la tarjeta del detalle del equipo (#228) puede reutilizarla
        var solicitudes = await _context.Solicitudes
            .AsNoTracking()
            .Where(s => s.NroEquipoRequerido == nroEquipo)
            .OrderBy(s => s.FechaSolicitud)
            .ThenBy(s => s.NroSolicitud)
            .Select(s => new
            {
                s.NroSolicitud,
                s.Usuario.RazonSocial,
                s.Usuario.Nombre,
                s.Usuario.Apellido,
                s.TipoSolicitante,
                s.OrigenSolicitud,
                s.FechaSolicitud,
                s.EstadoSolicitud,
                s.FechaCancelacion
            })
            .ToListAsync();

        var asignaciones = await _context.Asignaciones
            .AsNoTracking()
            .Where(a => a.NroEquipo == nroEquipo)
            .OrderBy(a => a.FechaAsignacion)
            .ThenBy(a => a.NroAsignacion)
            .Select(a => new AsignacionLegajoViewModel
            {
                NroAsignacion = a.NroAsignacion,
                NroSolicitud = a.NroSolicitud,
                Coordinador = a.Coordinador.Nombre + " " + a.Coordinador.Apellido,
                FechaAsignacion = a.FechaAsignacion,
                FechaLimiteRetiro = a.FechaLimiteRetiro,
                EstadoAsignacion = a.EstadoAsignacion,
                FechaLiberacion = a.FechaLiberacion,
                MotivoLiberacion = a.MotivoLiberacion
            })
            .ToListAsync();

        // el documento de quien retiró no se proyecta
        var entrega = await _context.Entregas
            .AsNoTracking()
            .Where(e => e.NroEquipo == nroEquipo)
            .OrderByDescending(e => e.FechaEntrega)
            .Select(e => new EntregaLegajoViewModel
            {
                NroEntrega = e.NroEntrega,
                NroAsignacion = e.NroAsignacion,
                NroSolicitud = e.NroSolicitud,
                Coordinador = e.Coordinador.Nombre + " " + e.Coordinador.Apellido,
                FechaEntrega = e.FechaEntrega
            })
            .FirstOrDefaultAsync();

        var filasSolicitudes = solicitudes.Select(s => new SolicitudLegajoViewModel
        {
            NroSolicitud = s.NroSolicitud,
            Solicitante = !string.IsNullOrWhiteSpace(s.RazonSocial) ? s.RazonSocial : $"{s.Nombre} {s.Apellido}".Trim(),
            TipoSolicitante = s.TipoSolicitante,
            OrigenSolicitud = s.OrigenSolicitud,
            FechaSolicitud = s.FechaSolicitud,
            EstadoSolicitud = s.EstadoSolicitud,
            FechaCancelacion = s.FechaCancelacion
        }).ToList();

        var donante = equipo.TipoUsuario == TipoUsuario.Persona
            ? $"{equipo.Nombre} {equipo.Apellido}".Trim()
            : equipo.RazonSocial ?? string.Empty;

        return new LegajoEquipoViewModel
        {
            NroEquipo = equipo.NroEquipo,
            TipoEquipo = equipo.TipoEquipo,
            Marca = equipo.Marca,
            Modelo = equipo.Modelo,
            EstadoInicialDeclarado = equipo.EstadoInicialDeclarado,
            Observaciones = equipo.Observaciones,
            EstadoEquipo = equipo.EstadoEquipo,
            FechaRegistro = equipo.FechaRegistro,
            FechaDisponibilidad = equipo.FechaDisponibilidad,
            FechaCancelacion = equipo.FechaCancelacion,
            MotivoCancelacion = equipo.MotivoCancelacion,
            Donante = donante,
            TipoCuentaDonante = equipo.TipoUsuario,
            Diagnosticos = diagnosticos,
            Solicitudes = filasSolicitudes,
            Asignaciones = asignaciones,
            Entrega = entrega,
            LineaDeTiempo = ArmarLineaDeTiempo(
                equipo.FechaRegistro, equipo.FechaDisponibilidad, equipo.FechaCancelacion,
                diagnosticos, filasSolicitudes, asignaciones, entrega)
        };
    }

    private static List<HitoViewModel> ArmarLineaDeTiempo(
        DateTime fechaRegistro, DateTime? fechaDisponibilidad, DateTime? fechaCancelacion,
        IEnumerable<DiagnosticoFilaViewModel> diagnosticos, IEnumerable<SolicitudLegajoViewModel> solicitudes,
        IEnumerable<AsignacionLegajoViewModel> asignaciones, EntregaLegajoViewModel? entrega)
    {
        var hitos = new List<HitoViewModel>
        {
            new() { Titulo = "Donación registrada", Fecha = fechaRegistro, Icono = "gift" }
        };

        hitos.AddRange(diagnosticos.Select(d => new HitoViewModel
        {
            Titulo = d.TipoDiagnostico switch
            {
                TipoDiagnostico.Rectificacion => $"Rectificación {d.CodigoDiagnostico}",
                TipoDiagnostico.Segundo => $"Segundo diagnóstico {d.CodigoDiagnostico}",
                _ => $"Diagnóstico {d.CodigoDiagnostico}"
            },
            Fecha = d.FechaDiagnostico,
            Icono = d.TipoDiagnostico == TipoDiagnostico.Rectificacion ? "edit" : "wrench"
        }));

        if (fechaDisponibilidad is not null)
        {
            hitos.Add(new() { Titulo = "Disponible en el catálogo", Fecha = fechaDisponibilidad, Icono = "check" });
        }

        foreach (var solicitud in solicitudes)
        {
            hitos.Add(new() { Titulo = $"Solicitud {solicitud.Codigo} de {solicitud.Solicitante}", Fecha = solicitud.FechaSolicitud, Icono = "tray" });

            if (solicitud.FechaCancelacion is not null)
            {
                hitos.Add(new() { Titulo = $"Solicitud {solicitud.Codigo} cancelada", Fecha = solicitud.FechaCancelacion, Icono = "x-circle", Estado = EstadoHito.Cancelado });
            }
        }

        foreach (var asignacion in asignaciones)
        {
            hitos.Add(new() { Titulo = $"Asignación {asignacion.Codigo} a {asignacion.CodigoSolicitud}", Fecha = asignacion.FechaAsignacion, Icono = "swap" });

            if (asignacion.FechaLiberacion is not null)
            {
                hitos.Add(new()
                {
                    Titulo = $"Asignación {asignacion.Codigo} liberada",
                    Fecha = asignacion.FechaLiberacion,
                    Icono = "undo",
                    Estado = EstadoHito.Cancelado,
                    Texto = asignacion.MotivoLiberacion
                });
            }
        }

        if (entrega is not null)
        {
            hitos.Add(new() { Titulo = $"Entrega {entrega.Codigo}", Fecha = entrega.FechaEntrega, Icono = "truck" });
        }

        if (fechaCancelacion is not null)
        {
            hitos.Add(new() { Titulo = "Donación cancelada", Fecha = fechaCancelacion, Icono = "x-circle", Estado = EstadoHito.Cancelado });
        }

        // orden estable: a igual fecha se respeta el orden en que se agregaron
        return hitos.OrderBy(h => h.Fecha).ToList();
    }
}
