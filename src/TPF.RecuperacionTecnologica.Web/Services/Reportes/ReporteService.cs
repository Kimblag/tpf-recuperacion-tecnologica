using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Asignaciones;
using TPF.RecuperacionTecnologica.Web.Services.Reloj;
using TPF.RecuperacionTecnologica.Web.ViewModels.Administrador;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

namespace TPF.RecuperacionTecnologica.Web.Services.Reportes;

public class ReporteService : IReporteService
{
    private static readonly CultureInfo Espanol = CultureInfo.GetCultureInfo("es-AR");

    private readonly ApplicationDbContext _context;
    private readonly IRelojInstitucional _reloj;

    public ReporteService(ApplicationDbContext context, IRelojInstitucional reloj)
    {
        _context = context;
        _reloj = reloj;
    }

    public async Task<IReadOnlyList<EquipoEstadoCantidadViewModel>> EquiposPorEstadoAsync(DateTime? desde, DateTime? hasta)
    {
        var (inicio, fin) = Rango(desde, hasta);

        var consulta = _context.Equipos.AsNoTracking();

        if (inicio is not null)
        {
            consulta = consulta.Where(e => e.FechaRegistro >= inicio);
        }

        if (fin is not null)
        {
            consulta = consulta.Where(e => e.FechaRegistro < fin);
        }

        // el conteo se resuelve en la base: solo vuelve una fila por estado
        var conteos = await consulta
            .GroupBy(e => e.EstadoEquipo)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        return conteos
            .OrderBy(c => c.Estado)
            .Select(c => new EquipoEstadoCantidadViewModel { Estado = c.Estado, Cantidad = c.Cantidad })
            .ToList();
    }

    public async Task<IReadOnlyList<PeriodoEntregasViewModel>> EntregasPorPeriodoAsync(
        DateTime? desde, DateTime? hasta, AgrupacionPeriodo agrupacion)
    {
        var (inicio, fin) = Rango(desde, hasta);

        var consulta = _context.Entregas.AsNoTracking();

        if (inicio is not null)
        {
            consulta = consulta.Where(e => e.FechaEntrega >= inicio);
        }

        if (fin is not null)
        {
            consulta = consulta.Where(e => e.FechaEntrega < fin);
        }

        // se obtienen todas las fechas de entrega para agruparlas en memoria,
        // ya que la agrupación depende de la zona horaria institucional
        var fechas = await consulta.Select(e => e.FechaEntrega).ToListAsync();

        var porMes = fechas
            .Select(f => _reloj.ALocal(f))
            .GroupBy(f => new { f.Year, f.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Cantidad = g.Count() })
            .ToList();

        if (porMes.Count == 0 && (inicio is null || fin is null))
        {
            return [];
        }

        // si no se especifica un rango, no devuelve nada
        if (porMes.Count == 0 && (desde is null || hasta is null))
        {
            return [];
        }

        // utiliza la fecha elegida por la persona y no los l[imites UTC
        var primerMes = desde?.Date ?? porMes.Min(m => new DateTime(m.Year, m.Month, 1));
        var ultimoMes = hasta?.Date ?? porMes.Max(m => new DateTime(m.Year, m.Month, 1));

        var cantidades = porMes.ToDictionary(m => (m.Year, m.Month), m => m.Cantidad);
        var filas = new List<PeriodoEntregasViewModel>();

        // se recorren todos los meses del rango para que los períodos sin entregas aparezcan con cero
        for (var mes = new DateTime(primerMes.Year, primerMes.Month, 1); mes <= ultimoMes; mes = mes.AddMonths(1))
        {
            var etiqueta = Etiqueta(mes, agrupacion);
            var cantidad = cantidades.GetValueOrDefault((mes.Year, mes.Month));

            if (filas.Count > 0 && filas[^1].Etiqueta == etiqueta)
            {
                filas[^1].Cantidad += cantidad;
            }
            else
            {
                filas.Add(new PeriodoEntregasViewModel { Etiqueta = etiqueta, Cantidad = cantidad });
            }
        }

        return filas;
    }

    public async Task<IReadOnlyList<TipoEquipoSolicitudesViewModel>> TiposSolicitadosAsync(DateTime? desde, DateTime? hasta)
    {
        var (inicio, fin) = Rango(desde, hasta);

        var consulta = _context.Solicitudes.AsNoTracking();

        if (inicio is not null)
        {
            consulta = consulta.Where(s => s.FechaSolicitud >= inicio);
        }

        if (fin is not null)
        {
            consulta = consulta.Where(s => s.FechaSolicitud < fin);
        }

        return await consulta
            .GroupBy(s => s.EquipoRequerido.TipoEquipo)
            .Select(g => new TipoEquipoSolicitudesViewModel { TipoEquipo = g.Key, Solicitudes = g.Count() })
            .OrderByDescending(f => f.Solicitudes)
            .ThenBy(f => f.TipoEquipo)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<FilaTrazabilidadViewModel>> TrazabilidadPorEquipoAsync(DateTime? desde, DateTime? hasta)
    {
        var (inicio, fin) = Rango(desde, hasta);

        var consulta = _context.Equipos.AsNoTracking();

        if (inicio is not null)
        {
            consulta = consulta.Where(e => e.FechaRegistro >= inicio);
        }

        if (fin is not null)
        {
            consulta = consulta.Where(e => e.FechaRegistro < fin);
        }

        var equipos = await consulta
            .OrderBy(e => e.NroEquipo)
            .Select(e => new
            {
                e.NroEquipo,
                e.TipoEquipo,
                e.Marca,
                e.Modelo,
                e.EstadoEquipo,
                e.FechaRegistro,
                TipoDonante = e.UsuarioDonante.TipoUsuario,
                NombreDonante = e.UsuarioDonante.Nombre,
                ApellidoDonante = e.UsuarioDonante.Apellido,
                RazonSocialDonante = e.UsuarioDonante.RazonSocial
            })
            .ToListAsync();

        if (equipos.Count == 0)
        {
            return [];
        }

        var numeros = equipos.Select(e => e.NroEquipo).ToList();

        var diagnosticos = (await _context.Diagnosticos
            .AsNoTracking()
            .Where(d => numeros.Contains(d.NroEquipo) && d.EsVigente)
            .Select(d => new
            {
                d.NroEquipo,
                Tecnico = d.Tecnico.Nombre + " " + d.Tecnico.Apellido,
                d.DictamenTecnico
            })
            .ToListAsync())
            .GroupBy(d => d.NroEquipo)
            .ToDictionary(g => g.Key, g => g.First());

        var asignaciones = (await _context.Asignaciones
            .AsNoTracking()
            .Where(a => numeros.Contains(a.NroEquipo) && a.EstadoAsignacion != EstadoAsignacion.Liberada)
            .Select(a => new
            {
                a.NroEquipo,
                Coordinador = a.Coordinador.Nombre + " " + a.Coordinador.Apellido,
                a.FechaAsignacion,
                a.Solicitud.TipoSolicitante,
                TipoBeneficiario = a.Solicitud.Usuario.TipoUsuario,
                NombreBeneficiario = a.Solicitud.Usuario.Nombre,
                ApellidoBeneficiario = a.Solicitud.Usuario.Apellido,
                RazonSocialBeneficiario = a.Solicitud.Usuario.RazonSocial
            })
            .ToListAsync())
            .GroupBy(a => a.NroEquipo)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.FechaAsignacion).First());

        var entregas = (await _context.Entregas
            .AsNoTracking()
            .Where(e => numeros.Contains(e.NroEquipo))
            .Select(e => new
            {
                e.NroEquipo,
                Coordinador = e.Coordinador.Nombre + " " + e.Coordinador.Apellido,
                e.FechaEntrega,
                e.NombreQuienRetira
            })
            .ToListAsync())
            .GroupBy(e => e.NroEquipo)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.FechaEntrega).First());

        const string sinDato = "—";

        return equipos
            .Select(e =>
            {
                diagnosticos.TryGetValue(e.NroEquipo, out var diagnostico);
                asignaciones.TryGetValue(e.NroEquipo, out var asignacion);
                entregas.TryGetValue(e.NroEquipo, out var entrega);

                return new FilaTrazabilidadViewModel
                {
                    Equipo = $"{e.NroEquipo:D6} · {NombreEnum.De(e.TipoEquipo)} {e.Marca} {e.Modelo}".Trim(),
                    Estado = NombreEnum.De(e.EstadoEquipo),
                    FechaRegistro = FormatearFecha(e.FechaRegistro),
                    Donante = NombreVisible(e.TipoDonante, e.NombreDonante, e.ApellidoDonante, e.RazonSocialDonante),
                    Diagnostico = diagnostico is null
                        ? sinDato
                        : $"{diagnostico.Tecnico} · {NombreEnum.De(diagnostico.DictamenTecnico)}",
                    Asignacion = asignacion is null
                        ? sinDato
                        : $"{asignacion.Coordinador} · {FormatearFecha(asignacion.FechaAsignacion)}",
                    Beneficiario = asignacion is null
                        ? sinDato
                        : $"{NombreEnum.De(asignacion.TipoSolicitante)} · {NombreVisible(asignacion.TipoBeneficiario, asignacion.NombreBeneficiario, asignacion.ApellidoBeneficiario, asignacion.RazonSocialBeneficiario)}",
                    Entrega = entrega is null
                        ? sinDato
                        : $"{entrega.Coordinador} · {FormatearFecha(entrega.FechaEntrega)}",
                    Retiro = entrega is null ? sinDato : entrega.NombreQuienRetira
                };
            })
            .ToList();
    }

    public async Task<IReadOnlyList<FilaExcepcionViewModel>> ExcepcionesAsync(DateTime? desde, DateTime? hasta)
    {
        var (inicio, fin) = Rango(desde, hasta);

        var justificadas = _context.Asignaciones
            .AsNoTracking()
            .Where(a => a.JustificacionCambio != null);

        if (inicio is not null)
        {
            justificadas = justificadas.Where(a => a.FechaAsignacion >= inicio);
        }

        if (fin is not null)
        {
            justificadas = justificadas.Where(a => a.FechaAsignacion < fin);
        }

        var liberadas = _context.Asignaciones
            .AsNoTracking()
            .Where(a => a.EstadoAsignacion == EstadoAsignacion.Liberada && a.FechaLiberacion != null);

        if (inicio is not null)
        {
            liberadas = liberadas.Where(a => a.FechaLiberacion >= inicio);
        }

        if (fin is not null)
        {
            liberadas = liberadas.Where(a => a.FechaLiberacion < fin);
        }

        var filasJustificadas = await justificadas
            .Select(a => new
            {
                Fecha = a.FechaAsignacion,
                a.NroEquipo,
                a.Equipo.Marca,
                a.Equipo.Modelo,
                a.NroSolicitud,
                Coordinador = a.Coordinador.Nombre + " " + a.Coordinador.Apellido,
                Detalle = a.JustificacionCambio
            })
            .ToListAsync();

        var filasLiberadas = await liberadas
            .Select(a => new
            {
                Fecha = a.FechaLiberacion!.Value,
                a.NroEquipo,
                a.Equipo.Marca,
                a.Equipo.Modelo,
                a.NroSolicitud,
                Coordinador = a.Coordinador.Nombre + " " + a.Coordinador.Apellido,
                Detalle = a.MotivoLiberacion
            })
            .ToListAsync();

        var resultado = new List<FilaExcepcionViewModel>();

        resultado.AddRange(filasJustificadas.Select(f => new FilaExcepcionViewModel
        {
            FechaUtc = f.Fecha,
            Fecha = FormatearFecha(f.Fecha),
            Tipo = "Asignación fuera de la prioridad sugerida",
            Equipo = $"{f.NroEquipo:D6} · {f.Marca} {f.Modelo}",
            Solicitud = $"SOL-{f.NroSolicitud:D6}",
            Coordinador = f.Coordinador,
            Detalle = f.Detalle ?? string.Empty
        }));

        resultado.AddRange(filasLiberadas.Select(f => new FilaExcepcionViewModel
        {
            FechaUtc = f.Fecha,
            Fecha = FormatearFecha(f.Fecha),
            Tipo = f.Detalle == AsignacionService.MotivoLiberacionAutomatica
                ? "Liberación por vencimiento"
                : "Liberación manual",
            Equipo = $"{f.NroEquipo:D6} · {f.Marca} {f.Modelo}",
            Solicitud = $"SOL-{f.NroSolicitud:D6}",
            Coordinador = f.Coordinador,
            Detalle = f.Detalle ?? string.Empty
        }));

        return resultado
            .OrderBy(f => f.FechaUtc)
            .ThenBy(f => f.Equipo)
            .ToList();
    }

    private string FormatearFecha(DateTime instanteUtc) =>
        _reloj.FechaLocal(instanteUtc).ToString("dd/MM/yyyy", Espanol);

    private static string NombreVisible(TipoUsuario tipo, string? nombre, string? apellido, string? razonSocial) =>
        tipo == TipoUsuario.Persona ? $"{nombre} {apellido}".Trim() : razonSocial ?? string.Empty;

    // devuelve el inicio del primer día y el inicio del día siguiente al último, para comparar con >= y <
    private (DateTime? Inicio, DateTime? Fin) Rango(DateTime? desde, DateTime? hasta)
    {
        if (desde is not null && hasta is not null && desde.Value.Date > hasta.Value.Date)
        {
            throw new ArgumentException("La fecha hasta no puede ser anterior a la fecha desde.", nameof(hasta));
        }

        return (
            desde is { } d ? _reloj.InicioDeDiaUtc(d) : (DateTime?)null,
            hasta is { } h ? _reloj.InicioDeDiaUtc(h.AddDays(1)) : (DateTime?)null);
    }

    private static string Etiqueta(DateTime mes, AgrupacionPeriodo agrupacion) => agrupacion switch
    {
        AgrupacionPeriodo.Anio => mes.Year.ToString(),
        AgrupacionPeriodo.Trimestre => $"{OrdinalTrimestre((mes.Month - 1) / 3 + 1)} trimestre {mes.Year}",
        _ => $"{Espanol.TextInfo.ToTitleCase(Espanol.DateTimeFormat.GetMonthName(mes.Month))} {mes.Year}"
    };

    private static string OrdinalTrimestre(int trimestre) => trimestre switch
    {
        1 => "1.er",
        2 => "2.º",
        3 => "3.er",
        _ => "4.º"
    };
}