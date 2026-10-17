using TPF.RecuperacionTecnologica.Web.ViewModels.Home;

namespace TPF.RecuperacionTecnologica.Web.Services.Home;

public interface IInicioService
{
    Task<InicioUsuarioViewModel?> ObtenerInicioUsuarioAsync(string identityUserId);

    /// <summary>
    /// Arma el resumen de inicio del Técnico: contadores de la cola de diagnóstico, los equipos que esperan
    /// una acción, del más antiguo al más reciente, y los últimos diagnósticos propios.
    /// Los diagnósticos del mes y la actividad reciente son solo los del Técnico de la cuenta.
    /// </summary>
    /// <returns>El resumen, o null si la cuenta no corresponde a un integrante del personal.</returns>
    Task<TecnicoInicioViewModel?> ObtenerInicioTecnicoAsync(string identityUserId);
}
