namespace TPF.RecuperacionTecnologica.Web.Services.Reloj;

/// <summary>
/// Define qué es "hoy" para los plazos de retiro, con la zona horaria de la institución.
/// Todas las pantallas y procesos que comparan una fecha límite usan este mismo criterio.
/// </summary>
public interface IRelojInstitucional
{
    /// <summary>Instante actual en UTC.</summary>
    DateTime AhoraUtc { get; }

    /// <summary>Fecha de hoy en la zona horaria de la institución (sin hora).</summary>
    DateTime Hoy { get; }

    /// <summary>
    /// Instante UTC en que empieza el día de la institución que está <paramref name="diasDesdeHoy"/> días de hoy.
    /// Sirve para filtrar en la base: un plazo que cae antes de <c>InicioDeDiaUtc(0)</c> es de un día anterior a hoy.
    /// </summary>
    DateTime InicioDeDiaUtc(int diasDesdeHoy = 0);

    /// <summary>Fecha en la zona horaria de la institución (sin hora) de un instante guardado en UTC.</summary>
    DateTime FechaLocal(DateTime instanteUtc);

    /// <summary>
    /// El plazo de retiro vence el día de su fecha límite: se puede entregar hasta el final de ese día
    /// y recién al día siguiente está vencido.
    /// </summary>
    bool PlazoVencido(DateTime fechaLimiteRetiro);
}
