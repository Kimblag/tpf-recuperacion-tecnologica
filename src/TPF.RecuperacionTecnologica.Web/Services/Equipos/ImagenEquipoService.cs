using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.Services.Equipos;

public class ImagenEquipoService : IImagenEquipoService
{
    public const long TamanoMaximoBytes = 5 * 1024 * 1024;

    private const string CarpetaRelativa = "uploads/equipos";

    private static readonly byte[] FirmaJpg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] FirmaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _entorno;
    private readonly ILogger<ImagenEquipoService> _logger;

    public ImagenEquipoService(
        ApplicationDbContext context,
        IWebHostEnvironment entorno,
        ILogger<ImagenEquipoService> logger)
    {
        _context = context;
        _entorno = entorno;
        _logger = logger;
    }

    public async Task<IdentityResult> CargarAsync(CargaImagenViewModel modelo)
    {
        if (modelo.Archivo is not { Length: > 0 } archivo)
        {
            return Fallo("ArchivoRequerido", "Seleccioná la imagen a cargar.");
        }

        if (archivo.Length > TamanoMaximoBytes)
        {
            return Fallo("ArchivoMuyGrande", "La imagen supera el tamaño máximo de 5 MB.");
        }

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

        if (extension is not (".jpg" or ".jpeg" or ".png"))
        {
            return Fallo("FormatoInvalido", "Solo se aceptan imágenes JPG o PNG.");
        }

        // la extensión la elige quien sube el archivo, así que también se verifica el contenido
        if (!await CoincideConFirmaAsync(archivo, extension == ".png" ? FirmaPng : FirmaJpg))
        {
            return Fallo("FormatoInvalido", "El archivo no es una imagen JPG o PNG válida.");
        }

        var equipo = await _context.Equipos.AsNoTracking()
            .Where(e => e.NroEquipo == modelo.NroEquipo)
            .Select(e => new { e.NroEquipo, e.EstadoEquipo })
            .FirstOrDefaultAsync();

        if (equipo is null) return Fallo("EquipoNoEncontrado", "No se encontró el equipo.");

        if (equipo.EstadoEquipo == EstadoEquipo.Cancelado)
        {
            return Fallo("EquipoCancelado", "La donación fue cancelada. No se pueden cargar imágenes del equipo.");
        }

        // el nombre lo genera el sistema: el original no se usa para evitar rutas o colisiones
        var nombre = $"{Guid.NewGuid():N}{(extension == ".png" ? ".png" : ".jpg")}";
        var carpeta = Path.Combine(_entorno.WebRootPath, CarpetaRelativa, equipo.NroEquipo.ToString());
        var rutaFisica = Path.Combine(carpeta, nombre);

        try
        {
            Directory.CreateDirectory(carpeta);

            await using (var destino = File.Create(rutaFisica))
            {
                await archivo.CopyToAsync(destino);
            }

            _context.ImagenesEquipo.Add(new ImagenEquipo
            {
                NroEquipo = equipo.NroEquipo,
                UrlImagen = $"~/{CarpetaRelativa}/{equipo.NroEquipo}/{nombre}",
                Angulo = string.IsNullOrWhiteSpace(modelo.Angulo) ? null : modelo.Angulo.Trim(),
                FechaCarga = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException or IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "No se pudo guardar la imagen del equipo {NroEquipo}", equipo.NroEquipo);

            // no se deja un archivo sin registro asociado
            if (File.Exists(rutaFisica)) File.Delete(rutaFisica);

            return Fallo("ErrorAlGuardar", "No se pudo guardar la imagen. Intentá nuevamente.");
        }

        return IdentityResult.Success;
    }

    private static async Task<bool> CoincideConFirmaAsync(IFormFile archivo, byte[] firma)
    {
        var encabezado = new byte[firma.Length];

        await using var flujo = archivo.OpenReadStream();
        var leidos = await flujo.ReadAtLeastAsync(encabezado, encabezado.Length, throwOnEndOfStream: false);

        return leidos == firma.Length && encabezado.AsSpan().SequenceEqual(firma);
    }

    private static IdentityResult Fallo(string codigo, string descripcion) =>
        IdentityResult.Failed(new IdentityError { Code = codigo, Description = descripcion });
}
