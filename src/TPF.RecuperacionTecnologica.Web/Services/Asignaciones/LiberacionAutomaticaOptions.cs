namespace TPF.RecuperacionTecnologica.Web.Services.Asignaciones;

public class LiberacionAutomaticaOptions
{
    public const string Seccion = "Asignaciones:LiberacionAutomatica";

    /// <summary>Si es false, no se libera nada solo: el Coordinador sigue pudiendo hacerlo a mano.</summary>
    public bool Habilitada { get; set; } = true;

    public int IntervaloMinutos { get; set; } = 60;
}
