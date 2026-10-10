namespace TPF.RecuperacionTecnologica.Web.Services.Cuenta;

public interface ICuentaService
{
    /// <summary>
    /// Verifica las credenciales y el estado de la cuenta, y recién entonces inicia la sesión.
    /// </summary>
    Task<ResultadoInicioSesion> IniciarSesionAsync(string correoElectronico, string password, bool recordarSesion);

    /// <summary>
    /// Cierra la sesión y audita el cierre de la cuenta indicada.
    /// </summary>
    Task CerrarSesionAsync(string? identityUserId);
}