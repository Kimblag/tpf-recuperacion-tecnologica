using Microsoft.AspNetCore.Identity;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Catalogo;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;
using TPF.RecuperacionTecnologica.Web.ViewModels.Donaciones;

namespace TPF.RecuperacionTecnologica.Web.Services.Donaciones;

public interface IDonacionService
{
    /// <summary>
    /// Registra la donación de un Equipo por parte del Usuario asociado a la cuenta de Identity.
    /// El Equipo queda Pendiente de recepción y el evento queda auditado sin Personal.
    /// </summary>
    /// <returns>El Número de Equipo generado, o null si la cuenta no corresponde a un Usuario activo.</returns>
    Task<int?> RegistrarAsync(string identityUserId, RegistroDonacionViewModel modelo);

    /// <summary>
    /// Devuelve los datos de una donación para editarla.
    /// </summary>
    /// <returns>
    /// Los datos de la donación, o null si no existe o no pertenece al Usuario de la cuenta.
    /// No distingue entre ambos casos para no revelar qué números de equipo existen.
    /// </returns>
    Task<EdicionDonacionViewModel?> ObtenerParaEdicionAsync(string identityUserId, int nroEquipo);

    /// <summary>
    /// Edita los datos declarados de una donación propia mientras el Equipo está Pendiente de recepción.
    /// No se puede cambiar el tipo de equipo. El evento queda auditado sin Personal.
    /// </summary>
    /// <returns>
    /// Éxito, o el motivo por el que no se pudo editar: la donación no existe o es de otro Usuario,
    /// ya no está Pendiente de recepción, o cambió mientras se editaba.
    /// </returns>
    Task<IdentityResult> EditarAsync(string identityUserId, EdicionDonacionViewModel modelo);

    /// <summary>
    /// Lista las donaciones propias, con filtros opcionales de búsqueda y estado de equipo.
    /// </summary>
    /// <returns>
    /// La lista de donaciones propias, vacía si no hay ninguna que cumpla los filtros.
    /// </returns>
    Task<ResultadoPaginado<DonacionFilaViewModel>> ListarAsync(
        string identityUserId, string? busqueda, EstadoEquipo? estado, TipoEquipo? tipo,
        EstadoInicialDeclarado? condicion, OrdenDonacion orden, int pagina, int tamanoPagina);

    /// <summary>
    /// Devuelve los datos de una donación propia para ver su detalle.
    /// </summary>
    /// <returns>
    /// Los datos de la donación, o null si no existe o no pertenece al Usuario de la cuenta.
    /// </returns>
    Task<DonacionDetalleViewModel?> ObtenerDetalleAsync(string identityUserId, int nroEquipo);

    /// <summary>
    /// Lista las donaciones de todos los Usuarios para el personal interno, de la más reciente a la más antigua,
    /// con filtros opcionales de búsqueda (número, marca o modelo), donante (nombre, razón social o correo) y estado de equipo.
    /// </summary>
    /// <returns>
    /// La página pedida, ajustada al rango válido, o vacía si ninguna donación cumple los filtros.
    /// </returns>
    Task<ResultadoPaginado<DonacionGestionFilaViewModel>> ListarTodasAsync(
        string? busqueda, string? donante, EstadoEquipo? estado, int pagina, int tamanoPagina);

    /// <summary>
    /// Devuelve los datos de una donación de cualquier Usuario, en cualquier estado, para consulta del personal interno.
    /// </summary>
    /// <returns>Los datos de la donación, o null si no existe.</returns>
    Task<DonacionDetalleViewModel?> ObtenerDetalleInternoAsync(int nroEquipo);

    /// <summary>
    /// Cancela una donación propia mientras el Equipo está Pendiente de recepción.
    /// </summary>
    /// <returns>
    /// Éxito, o el motivo por el que no se pudo cancelar, si es que la donación no existe o es de otro Usuario
    /// </returns>
    Task<IdentityResult> CancelarAsync(string identityUserId, int nroEquipo);

    /// <summary>
    /// Lista los equipos Disponibles para el catálogo, con búsqueda, filtro por marca y tipo, orden y paginación.
    /// Las características son las verificadas en el diagnóstico vigente, no las declaradas por el donante.
    /// </summary>
    /// <returns>
    /// La página pedida, ajustada al rango válido, junto con el total de equipos disponibles sin filtrar
    /// y las marcas disponibles para el filtro.
    /// </returns>
    Task<ResultadoCatalogo> ObtenerDisponiblesAsync(
        string? busqueda, string? marca, TipoEquipo? tipo, OrdenCatalogo orden, int pagina, int tamanoPagina);

    /// <summary>
    /// Devuelve el detalle de un equipo Disponible para el catálogo, con sus características verificadas y sus imágenes.
    /// </summary>
    /// <returns>Los datos del equipo, o null si no existe o no está Disponible.</returns>
    Task<EquipoCatalogoDetalleViewModel?> ObtenerDetalleDisponibleAsync(int nroEquipo);
}