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
}
