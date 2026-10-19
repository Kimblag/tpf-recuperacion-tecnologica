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
}
