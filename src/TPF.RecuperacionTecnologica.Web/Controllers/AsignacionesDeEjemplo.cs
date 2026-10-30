using TPF.RecuperacionTecnologica.Web.Models.Enums;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

internal sealed record AsignacionEjemplo(
    int NroAsignacion, int NroSolicitud, string Solicitante, TipoSolicitante TipoSolicitante,
    int NroEquipo, TipoEquipo TipoEquipo, string MarcaYModelo, string Coordinador,
    DateTime Fecha, DateTime Limite, EstadoAsignacion Estado, bool CambioPrioridad = false,
    string? Justificacion = null, DateTime? FechaLiberacion = null, string? MotivoLiberacion = null,
    DateTime? FechaEntrega = null);

// TODO: reemplazar estos datos de ejemplo por las entregas reales (#152); el listado de asignaciones ya no los usa
internal static class AsignacionesDeEjemplo
{
    private static readonly DateTime Hoy = DateTime.Today;

    public static readonly IReadOnlyList<AsignacionEjemplo> Todas =
    [
        new(18, 25, "Valeria Acosta", TipoSolicitante.Particular, 125, TipoEquipo.Notebook, "HP ProBook 450 G5", "Sofía Gutiérrez",
            Hoy.AddDays(-5), Hoy.AddDays(5), EstadoAsignacion.Vigente),
        new(17, 27, "Marcela Bustos", TipoSolicitante.Escuela, 135, TipoEquipo.Celular, "Samsung Galaxy A10", "Sofía Gutiérrez",
            Hoy.AddDays(-8), Hoy.AddDays(2), EstadoAsignacion.Vigente, true,
            "La escuela retira el equipo hoy mismo para empezar las clases de mañana; la otra persona solicitante no puede retirarlo esta semana."),
        new(19, 22, "Julieta Sosa", TipoSolicitante.Particular, 131, TipoEquipo.Celular, "Motorola Moto G7 Play", "Ramiro Ortega",
            Hoy.AddDays(-14), Hoy.AddDays(-4), EstadoAsignacion.Vigente),
        new(16, 19, "Valeria Acosta", TipoSolicitante.Particular, 129, TipoEquipo.PcEscritorio, "Lenovo ThinkCentre M710", "Sofía Gutiérrez",
            new DateTime(2026, 8, 14), new DateTime(2026, 8, 24), EstadoAsignacion.Concretada, FechaEntrega: new DateTime(2026, 8, 21)),
        new(15, 17, "Valeria Acosta", TipoSolicitante.Particular, 128, TipoEquipo.Periferico, "Logitech K120", "Ramiro Ortega",
            new DateTime(2026, 8, 10), new DateTime(2026, 8, 20), EstadoAsignacion.Liberada,
            FechaLiberacion: new DateTime(2026, 8, 21), MotivoLiberacion: "Venció el plazo de retiro."),
        new(14, 16, "Marcos Ibarra", TipoSolicitante.Particular, 120, TipoEquipo.Notebook, "Lenovo ThinkPad T480", "Ramiro Ortega",
            new DateTime(2026, 7, 30), new DateTime(2026, 8, 9), EstadoAsignacion.Concretada, FechaEntrega: new DateTime(2026, 8, 1)),
        new(13, 14, "Escuela Primaria N° 5", TipoSolicitante.Escuela, 122, TipoEquipo.Monitor, "LG 22MK430", "Sofía Gutiérrez",
            new DateTime(2026, 7, 25), new DateTime(2026, 8, 4), EstadoAsignacion.Concretada, FechaEntrega: new DateTime(2026, 7, 28))
    ];
}
