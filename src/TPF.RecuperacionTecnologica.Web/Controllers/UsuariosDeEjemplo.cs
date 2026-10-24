using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

// TODO: reemplazar por la búsqueda real de usuarios por DNI o correo, validando que estén Activos (#121)
internal static class UsuariosDeEjemplo
{
    private static readonly IReadOnlyList<UsuarioEncontradoViewModel> Usuarios =
    [
        new() { NroUsuario = 33, Nombre = "Valeria Acosta", Documento = "34.205.918", CorreoElectronico = "valeria.acosta@example.com", EstadoUsuario = EstadoUsuario.Activo },
        new() { NroUsuario = 41, Nombre = "Roberto Quiroga", Documento = "20.123.456", CorreoElectronico = "roberto.quiroga@example.com", EstadoUsuario = EstadoUsuario.Activo },
        new() { NroUsuario = 44, Nombre = "Ana Torres", Documento = "11.222.333", CorreoElectronico = "ana.torres@example.com", EstadoUsuario = EstadoUsuario.Inactivo }
    ];

    public static void Buscar(SelectorUsuarioViewModel usuario)
    {
        var texto = usuario.Busqueda;

        if (usuario.Modo != ModoUsuarioAsistido.Existente || string.IsNullOrEmpty(texto))
        {
            return;
        }

        var soloDigitos = new string(texto.Where(char.IsDigit).ToArray());

        usuario.BusquedaRealizada = true;
        usuario.Encontrado = Usuarios.FirstOrDefault(u =>
            string.Equals(u.CorreoElectronico, texto, StringComparison.OrdinalIgnoreCase)
            || (soloDigitos.Length > 0 && u.Documento.Replace(".", string.Empty) == soloDigitos));
        usuario.NroUsuario = usuario.Encontrado is { EstadoUsuario: EstadoUsuario.Activo } activo ? activo.NroUsuario : null;
    }
}
