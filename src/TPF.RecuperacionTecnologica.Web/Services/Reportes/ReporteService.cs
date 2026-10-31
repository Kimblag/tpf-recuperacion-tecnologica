using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.ViewModels.Administrador;
using TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

namespace TPF.RecuperacionTecnologica.Web.Services.Reportes;

public class ReporteService : IReporteService
{
    private static readonly CultureInfo Espanol = CultureInfo.GetCultureInfo("es-AR");

    private readonly ApplicationDbContext _context;

    public ReporteService(ApplicationDbContext context)
    {
        _context = context;
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

        // la base agrupa por año y mes; los trimestres y años se arman a partir de los meses
        var porMes = await consulta
            .GroupBy(e => new { e.FechaEntrega.Year, e.FechaEntrega.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Cantidad = g.Count() })
            .ToListAsync();

        if (porMes.Count == 0 && (inicio is null || fin is null))
        {
            return [];
        }

        // sin rango completo, los períodos van de la primera a la última entrega
        var primerMes = inicio ?? porMes.Min(m => new DateTime(m.Year, m.Month, 1));
        var ultimoMes = fin?.AddDays(-1) ?? porMes.Max(m => new DateTime(m.Year, m.Month, 1));

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

    // devuelve el inicio del primer día y el inicio del día siguiente al último, para comparar con >= y <
    private static (DateTime? Inicio, DateTime? Fin) Rango(DateTime? desde, DateTime? hasta)
    {
        if (desde is not null && hasta is not null && desde.Value.Date > hasta.Value.Date)
        {
            throw new ArgumentException("La fecha hasta no puede ser anterior a la fecha desde.", nameof(hasta));
        }

        return (desde?.Date, hasta?.Date.AddDays(1));
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
