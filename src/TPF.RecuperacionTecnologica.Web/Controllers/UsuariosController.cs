using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Usuarios;

namespace TPF.RecuperacionTecnologica.Web.Controllers;

[Authorize(Policy = "SoloAdministrador")]
public class UsuariosController : Controller
{
    private static readonly IReadOnlyList<UsuarioDetalleViewModel> DatosDeEjemplo =
    [
        Persona(31, "34205918", "Valeria", "Acosta", "valeria.acosta@example.com", "11 5550-3412", "Av. Mitre 1450, Avellaneda", new DateTime(2026, 3, 12)),
        Persona(32, "28330145", "Laura", "Pereyra", "laura.pereyra@example.com", "11 5550-3420", "Calle 9 de Julio 320, Quilmes", new DateTime(2026, 3, 20)),
        Persona(33, "36912504", "Julieta", "Sosa", "julieta.sosa@example.com", "11 5550-3433", "Belgrano 875, Lanús", new DateTime(2026, 4, 2)),
        Persona(34, "25640881", "Roberto", "Quiroga", "roberto.quiroga@example.com", "11 5550-3441", "Pasaje Los Aromos 12, Berazategui", new DateTime(2026, 4, 15)),
        Persona(35, "38105269", "Nicolás", "Herrera", "nicolas.herrera@example.com", "11 5550-3455", "Sarmiento 2210, Banfield", new DateTime(2026, 5, 30)),
        Persona(36, "31774320", "Martín", "Ledesma", "martin.ledesma@example.com", "11 5550-3467", "Rivadavia 640, Temperley", new DateTime(2026, 6, 18)),
        Institucion(37, "30111111119", "Escuela Primaria N° 5", "escuela5@example.com", "11 5550-3480", "Av. Hipólito Yrigoyen 900, Lomas de Zamora", new DateTime(2026, 7, 4))
    ];

    [HttpGet]
    public IActionResult Index()
    {
        return View(new UsuarioListadoViewModel
        {
            Filas = DatosDeEjemplo,
            Total = 128
        });
    }

    [HttpGet]
    public IActionResult Detalle(int id)
    {
        var usuario = DatosDeEjemplo.FirstOrDefault(u => u.NroUsuario == id);

        return usuario is null ? NotFound() : View(usuario);
    }

    private static UsuarioDetalleViewModel Persona(
        int nroUsuario, string dni, string nombre, string apellido,
        string correo, string telefono, string direccion, DateTime fechaAlta) => new()
        {
            NroUsuario = nroUsuario,
            TipoUsuario = TipoUsuario.Persona,
            Nombre = nombre,
            Apellido = apellido,
            TipoDocumento = TipoDocumento.Dni,
            NroDocumento = dni,
            CorreoElectronico = correo,
            Telefono = telefono,
            Direccion = direccion,
            FechaAlta = fechaAlta,
            EstadoUsuario = EstadoUsuario.Activo,
            DonacionesRegistradas = 6,
            SolicitudesRealizadas = 5,
            EntregasRecibidas = 1
        };

    private static UsuarioDetalleViewModel Institucion(
        int nroUsuario, string cuit, string razonSocial,
        string correo, string telefono, string direccion, DateTime fechaAlta) => new()
        {
            NroUsuario = nroUsuario,
            TipoUsuario = TipoUsuario.Institucion,
            RazonSocial = razonSocial,
            TipoDocumento = TipoDocumento.Cuit,
            NroDocumento = cuit,
            CorreoElectronico = correo,
            Telefono = telefono,
            Direccion = direccion,
            FechaAlta = fechaAlta,
            EstadoUsuario = EstadoUsuario.Activo,
            DonacionesRegistradas = 2,
            SolicitudesRealizadas = 3,
            EntregasRecibidas = 2
        };
}
