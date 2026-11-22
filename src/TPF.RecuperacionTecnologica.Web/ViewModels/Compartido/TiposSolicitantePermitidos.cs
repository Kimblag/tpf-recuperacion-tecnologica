using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

public static class TiposSolicitantePermitidos
{
    public static IReadOnlyList<TipoSolicitante> Para(TipoUsuario tipoUsuario) =>
        tipoUsuario == TipoUsuario.Persona
            ? [TipoSolicitante.Particular]
            : [TipoSolicitante.Escuela, TipoSolicitante.OrganizacionSocial];

    public static bool EsCoherente(TipoUsuario tipoUsuario, TipoSolicitante tipoSolicitante) =>
        Para(tipoUsuario).Contains(tipoSolicitante);
}
