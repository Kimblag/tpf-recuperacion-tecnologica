using Microsoft.Extensions.Options;

namespace TPF.RecuperacionTecnologica.Web.Services.Reloj;

public class RelojInstitucionalOptions
{
    public const string Seccion = "Institucion";

    /// <summary>Identificador de la zona horaria (IANA o de Windows).</summary>
    public string ZonaHoraria { get; set; } = "America/Argentina/Buenos_Aires";
}

public class RelojInstitucional : IRelojInstitucional
{
    private readonly TimeZoneInfo _zona;
    private readonly TimeProvider _tiempo;

    public RelojInstitucional(IOptions<RelojInstitucionalOptions> options, TimeProvider tiempo)
    {
        _tiempo = tiempo;

        var id = options.Value.ZonaHoraria;

        try
        {
            _zona = TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // fallar al arrancar es mejor que calcular mal los plazos con otra zona
            throw new InvalidOperationException(
                $"La zona horaria '{id}' configurada en Institucion:ZonaHoraria no es válida.", ex);
        }
    }

    public DateTime AhoraUtc => _tiempo.GetUtcNow().UtcDateTime;

    public DateTime Hoy => FechaLocal(AhoraUtc);

    public DateTime FechaLocal(DateTime instanteUtc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(instanteUtc, DateTimeKind.Utc), _zona).Date;

    public DateTime InicioDeDiaUtc(int diasDesdeHoy = 0)
    {
        var diaLocal = DateTime.SpecifyKind(Hoy.AddDays(diasDesdeHoy), DateTimeKind.Unspecified);

        return TimeZoneInfo.ConvertTimeToUtc(diaLocal, _zona);
    }

    public bool PlazoVencido(DateTime fechaLimiteRetiro) =>
        DateTime.SpecifyKind(fechaLimiteRetiro, DateTimeKind.Utc) < InicioDeDiaUtc();
}
