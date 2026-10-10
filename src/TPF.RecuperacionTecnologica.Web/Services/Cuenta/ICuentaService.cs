namespace TPF.RecuperacionTecnologica.Web.Services.Cuenta;

public interface ICuentaService
{
    /// <summary>
    /// Verifica las credenciales y el estado de la cuenta, y recién entonces inicia la sesión.
    /// </summary>
    Task<ResultadoInicioSesion> IniciarSesionAsync(string correoElectronico, string password, bool recordarSesion);

    Task CerrarSesionAsync();
}
