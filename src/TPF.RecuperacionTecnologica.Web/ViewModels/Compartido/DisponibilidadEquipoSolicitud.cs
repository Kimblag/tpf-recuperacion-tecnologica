using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

public static class DisponibilidadEquipoSolicitud
{
    public static bool EstaDisponible(EstadoEquipo estado) => estado == EstadoEquipo.Disponible;

    public static bool EsTemporal(EstadoEquipo estado) =>
        estado is EstadoEquipo.Asignado or EstadoEquipo.EnReparacion or EstadoEquipo.PendienteRecepcion;

    public static string? Mensaje(EstadoEquipo estado) => estado switch
    {
        EstadoEquipo.Disponible => null,
        EstadoEquipo.Asignado => "El equipo está asignado a otra solicitud. Esta queda en espera por si se libera.",
        EstadoEquipo.EnReparacion => "El equipo está en reparación. La solicitud queda en espera hasta que vuelva a estar disponible.",
        EstadoEquipo.PendienteRecepcion => "El equipo todavía está pendiente de recepción. La solicitud queda en espera hasta que esté disponible.",
        _ => $"El equipo ya no está disponible (estado {NombreEnum.De(estado)}). Cancelá la solicitud o comunicate con la persona."
    };

    public static string? Etiqueta(EstadoEquipo estado) => estado switch
    {
        EstadoEquipo.Disponible => null,
        _ when EsTemporal(estado) => $"En espera: equipo {NombreEnum.De(estado).ToLowerInvariant()}",
        _ => $"Equipo no disponible ({NombreEnum.De(estado)})"
    };
}
