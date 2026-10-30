using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.Services.Auditoria;

public interface IAuditoriaService
{
    /// <summary>
    /// Registra un evento de auditoría.
    /// Se debe llamar después de guardar la operación de negocio, para que
    /// <paramref name="idEntidadAfectada"/> ya exista y no se audite algo que falló.
    /// </summary>
    /// <param name="tipoOperacion">Tipo de operación realizada.</param>
    /// <param name="codigoPersonal">Personal que la ejecutó; null si el actor no es personal (donante, solicitante o sistema).</param>
    /// <param name="entidadAfectada">Nombre de la entidad afectada; usar nameof(Entidad).</param>
    /// <param name="idEntidadAfectada">Identificador de la entidad afectada.</param>
    /// <param name="detalle">Descripción corta opcional del evento.</param>
    Task RegistrarEventoAsync(TipoOperacionAuditoria tipoOperacion, int? codigoPersonal, string entidadAfectada, int idEntidadAfectada, string? detalle);

    /// <summary>
    /// Consulta la bitácora paginada, de la más reciente a la más antigua.
    /// Cada filtro se aplica solo si viene informado; <paramref name="hasta"/> incluye todo ese día.
    /// </summary>
    /// <exception cref="ArgumentException">Si <paramref name="desde"/> es posterior a <paramref name="hasta"/>.</exception>
    Task<ResultadoPaginado<AuditoriaFilaViewModel>> ConsultarAsync(
        DateTime? desde, DateTime? hasta, TipoOperacionAuditoria? tipoOperacion, int? codigoPersonal,
        string? entidadAfectada, int? idEntidadAfectada, int pagina, int tamanoPagina);
}
