using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.Services.Auditoria;

public class AuditoriaService : IAuditoriaService
{
    private readonly ApplicationDbContext _context;
    private const int DetalleMaxLength = 500;

    public AuditoriaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarEventoAsync(TipoOperacionAuditoria tipoOperacion, int? codigoPersonal, string entidadAfectada, int idEntidadAfectada, string? detalle)
    {
        // Limpiar y normalizar el detalle antes de guardarlo
        string? detalleNormalizado = detalle?.Trim();
        // si tine espacios, guardar null
        if (string.IsNullOrWhiteSpace(detalleNormalizado)) detalleNormalizado = null;

        // Si hay verificar su longitud antes de recortar a 500 caracteres
        if (detalleNormalizado != null && detalleNormalizado.Length > DetalleMaxLength)
        {
            detalleNormalizado = detalleNormalizado.Substring(0, DetalleMaxLength);
        }

        var registro = new RegistroAuditoria()
        {
            FechaHora = DateTime.UtcNow,
            TipoOperacion = tipoOperacion,
            CodigoPersonal = codigoPersonal,
            EntidadAfectada = entidadAfectada,
            IdEntidadAfectada = idEntidadAfectada,
            Detalle = detalleNormalizado
        };

        _context.RegistrosAuditoria.Add(registro);
        await _context.SaveChangesAsync();
    }

    public async Task<ResultadoPaginado<AuditoriaFilaViewModel>> ConsultarAsync(
        DateTime? desde, DateTime? hasta, TipoOperacionAuditoria? tipoOperacion, int? codigoPersonal,
        string? entidadAfectada, int? idEntidadAfectada, int pagina, int tamanoPagina)
    {
        if (desde is not null && hasta is not null && desde.Value.Date > hasta.Value.Date)
        {
            throw new ArgumentException("La fecha desde no puede ser posterior a la fecha hasta.", nameof(desde));
        }

        var consulta = _context.RegistrosAuditoria.AsNoTracking();

        if (desde is not null)
        {
            var inicio = desde.Value.Date;
            consulta = consulta.Where(r => r.FechaHora >= inicio);
        }

        if (hasta is not null)
        {
            // se incluye todo el día elegido
            var fin = hasta.Value.Date.AddDays(1);
            consulta = consulta.Where(r => r.FechaHora < fin);
        }

        if (tipoOperacion is not null)
        {
            consulta = consulta.Where(r => r.TipoOperacion == tipoOperacion);
        }

        if (codigoPersonal is not null)
        {
            consulta = consulta.Where(r => r.CodigoPersonal == codigoPersonal);
        }

        if (!string.IsNullOrWhiteSpace(entidadAfectada))
        {
            var entidad = entidadAfectada.Trim();
            consulta = consulta.Where(r => r.EntidadAfectada == entidad);
        }

        if (idEntidadAfectada is not null)
        {
            consulta = consulta.Where(r => r.IdEntidadAfectada == idEntidadAfectada);
        }

        var total = await consulta.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanoPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var registros = await consulta
            .OrderByDescending(r => r.FechaHora)
            .ThenByDescending(r => r.NroAuditoria)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(r => new
            {
                r.FechaHora,
                r.TipoOperacion,
                r.CodigoPersonal,
                Nombre = r.Personal != null ? r.Personal.Nombre : null,
                Apellido = r.Personal != null ? r.Personal.Apellido : null,
                r.EntidadAfectada,
                r.IdEntidadAfectada,
                r.Detalle
            })
            .ToListAsync();

        return new ResultadoPaginado<AuditoriaFilaViewModel>
        {
            Elementos = registros.Select(r => new AuditoriaFilaViewModel
            {
                FechaHora = r.FechaHora,
                CodigoPersonal = r.CodigoPersonal,
                NombrePersonal = $"{r.Nombre} {r.Apellido}".Trim(),
                Operacion = r.TipoOperacion,
                Entidad = r.EntidadAfectada,
                IdEntidad = r.IdEntidadAfectada,
                Identificador = AuditoriaFilaViewModel.IdentificadorDe(r.EntidadAfectada, r.IdEntidadAfectada),
                Detalle = r.Detalle
            }).ToList(),
            Total = total,
            Pagina = pagina
        };
    }
}
