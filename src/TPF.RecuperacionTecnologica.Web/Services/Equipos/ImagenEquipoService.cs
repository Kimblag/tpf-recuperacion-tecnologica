using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.Models;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.Services.Auditoria;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.Services.Equipos;

public class ImagenEquipoService : IImagenEquipoService
{
    private static readonly byte[] FirmaJpg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] FirmaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _entorno;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<ImagenEquipoService> _logger;
    private readonly ImagenesOptions _opciones;

    public ImagenEquipoService(
        ApplicationDbContext context,
        IWebHostEnvironment entorno,
        IAuditoriaService auditoriaService,
        IOptions<ImagenesOptions> opciones,
        ILogger<ImagenEquipoService> logger)
    {
        _context = context;
        _entorno = entorno;
        _auditoriaService = auditoriaService;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public static bool EstadoPermiteEditar(EstadoEquipo estado) =>
        estado is EstadoEquipo.PendienteRecepcion or EstadoEquipo.EnReparacion or EstadoEquipo.Disponible;

    public async Task<GestionFotosViewModel?> ObtenerGestionAsync(int nroEquipo)
    {
        var equipo = await _context.Equipos
            .AsNoTracking()
            .Where(e => e.NroEquipo == nroEquipo)
            .Select(e => new { e.NroEquipo, e.TipoEquipo, e.Marca, e.Modelo, e.EstadoEquipo })
            .FirstOrDefaultAsync();

        if (equipo is null)
        {
            return null;
        }

        var fotos = await _context.ImagenesEquipo
            .AsNoTracking()
            .Where(i => i.NroEquipo == nroEquipo)
            .OrderBy(i => i.FechaCarga)
            .ThenBy(i => i.NroImagen)
            .Select(i => new FotoGestionViewModel
            {
                NroImagen = i.NroImagen,
                UrlImagen = i.UrlImagen,
                Angulo = i.Angulo,
                NroDiagnostico = i.NroDiagnostico
            })
            .ToListAsync();

        var puedeEditar = EstadoPermiteEditar(equipo.EstadoEquipo);

        return new GestionFotosViewModel
        {
            NroEquipo = equipo.NroEquipo,
            TipoEquipo = equipo.TipoEquipo,
            Marca = equipo.Marca,
            Modelo = equipo.Modelo,
            EstadoEquipo = equipo.EstadoEquipo,
            Fotos = fotos,
            PuedeEditar = puedeEditar,
            MotivoNoEditable = puedeEditar ? null : MensajeNoEditable(equipo.EstadoEquipo),
            MaximoPorEquipo = _opciones.MaximoPorEquipo,
            MaximoPorEnvio = _opciones.MaximoPorEnvio,
            TamanoMaximoBytes = _opciones.TamanoMaximoBytes
        };
    }

    public async Task<ResultadoLoteImagenes> PrepararAsync(int nroEquipo, IReadOnlyList<ArchivoImagen> archivos)
    {
        var equipo = await _context.Equipos
            .AsNoTracking()
            .Where(e => e.NroEquipo == nroEquipo)
            .Select(e => new { e.NroEquipo, e.EstadoEquipo })
            .FirstOrDefaultAsync();

        if (equipo is null)
        {
            return Rechazo("EquipoNoEncontrado", "No se encontró el equipo.");
        }

        if (archivos.Count == 0)
        {
            return new ResultadoLoteImagenes();
        }

        if (archivos.Count > _opciones.MaximoPorEnvio)
        {
            return Rechazo("DemasiadasImagenes", $"Se pueden cargar hasta {_opciones.MaximoPorEnvio} imágenes por envío.");
        }

        var existentes = await _context.ImagenesEquipo.CountAsync(i => i.NroEquipo == nroEquipo);

        if (existentes + archivos.Count > _opciones.MaximoPorEquipo)
        {
            return Rechazo("LimiteDeImagenes",
                $"Cada equipo admite hasta {_opciones.MaximoPorEquipo} imágenes y este ya tiene {existentes}.");
        }

        foreach (var archivo in archivos)
        {
            var error = await ValidarAsync(archivo);

            if (error is not null)
            {
                return ResultadoLoteImagenes.Fallido(error);
            }
        }

        var carpeta = Path.Combine(_opciones.ResolverCarpetaFisica(_entorno.WebRootPath), nroEquipo.ToString());
        var imagenes = new List<ImagenEquipo>();
        var rutas = new List<string>();
        var ahora = DateTime.UtcNow;

        try
        {
            Directory.CreateDirectory(carpeta);

            foreach (var item in archivos)
            {
                var extension = Path.GetExtension(item.Archivo.FileName).ToLowerInvariant() == ".png" ? ".png" : ".jpg";
                var nombre = $"{Guid.NewGuid():N}{extension}";
                var ruta = Path.Combine(carpeta, nombre);

                rutas.Add(ruta);

                await using (var destino = File.Create(ruta))
                {
                    await item.Archivo.CopyToAsync(destino);
                }

                imagenes.Add(new ImagenEquipo
                {
                    NroEquipo = nroEquipo,
                    UrlImagen = $"~/{ImagenesOptions.RutaPublica}/{nroEquipo}/{nombre}",
                    Angulo = AngulosFoto.Normalizar(item.Angulo),
                    FechaCarga = ahora.AddMilliseconds(imagenes.Count)
                });
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "No se pudieron guardar las imágenes del equipo {NroEquipo}", nroEquipo);
            EliminarArchivos(rutas);

            return Rechazo("ErrorAlGuardar", "No se pudieron guardar las imágenes. Intentá nuevamente.");
        }

        return new ResultadoLoteImagenes { Imagenes = imagenes, Archivos = rutas };
    }

    public void DescartarArchivos(ResultadoLoteImagenes lote) => EliminarArchivos(lote.Archivos);

    public async Task<IdentityResult> AgregarAsync(int nroEquipo, IReadOnlyList<ArchivoImagen> archivos, int codigoTecnico)
    {
        if (archivos.Count == 0)
        {
            return Fallo("ArchivoRequerido", "Seleccioná al menos una imagen.");
        }

        var estado = await _context.Equipos
            .AsNoTracking()
            .Where(e => e.NroEquipo == nroEquipo)
            .Select(e => (EstadoEquipo?)e.EstadoEquipo)
            .FirstOrDefaultAsync();

        if (estado is null)
        {
            return Fallo("EquipoNoEncontrado", "No se encontró el equipo.");
        }

        if (!EstadoPermiteEditar(estado.Value))
        {
            return Fallo("EquipoNoEditable", MensajeNoEditable(estado.Value));
        }

        var lote = await PrepararAsync(nroEquipo, archivos);

        if (!lote.Resultado.Succeeded)
        {
            return lote.Resultado;
        }

        try
        {
            _context.ImagenesEquipo.AddRange(lote.Imagenes);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "No se pudieron registrar las imágenes del equipo {NroEquipo}", nroEquipo);
            DescartarArchivos(lote);

            return Fallo("ErrorAlGuardar", "No se pudieron guardar las imágenes. Intentá nuevamente.");
        }

        await AuditarAsync(codigoTecnico, nroEquipo,
            $"Carga de {archivos.Count} {(archivos.Count == 1 ? "imagen" : "imágenes")} del equipo {nroEquipo}");

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> EliminarAsync(int nroEquipo, int nroImagen, int codigoTecnico)
    {
        var imagen = await _context.ImagenesEquipo
            .Include(i => i.Equipo)
            .FirstOrDefaultAsync(i => i.NroImagen == nroImagen && i.NroEquipo == nroEquipo);

        if (imagen is null)
        {
            return Fallo("ImagenNoEncontrada", "No se encontró la foto.");
        }

        var estado = imagen.Equipo.EstadoEquipo;

        if (!EstadoPermiteEditar(estado))
        {
            return Fallo("EquipoNoEditable", MensajeNoEditable(estado));
        }

        if (estado == EstadoEquipo.Disponible
            && await _context.ImagenesEquipo.CountAsync(i => i.NroEquipo == nroEquipo) <= 1)
        {
            return Fallo("UltimaImagen",
                "Un equipo disponible debe conservar al menos una foto. Cargar otra antes de eliminar esta.");
        }

        var ruta = Path.Combine(
            _opciones.ResolverCarpetaFisica(_entorno.WebRootPath), nroEquipo.ToString(), Path.GetFileName(imagen.UrlImagen));

        try
        {
            _context.ImagenesEquipo.Remove(imagen);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "No se pudo eliminar la imagen {NroImagen} del equipo {NroEquipo}", nroImagen, nroEquipo);

            return Fallo("ErrorAlGuardar", "No se pudo eliminar la foto. Intentá nuevamente.");
        }

        EliminarArchivos([ruta]);

        await AuditarAsync(codigoTecnico, nroEquipo, $"Eliminación de una imagen del equipo {nroEquipo}");

        return IdentityResult.Success;
    }

    public async Task<IdentityResult> ActualizarAnguloAsync(int nroEquipo, int nroImagen, string? angulo, int codigoTecnico)
    {
        var imagen = await _context.ImagenesEquipo
            .Include(i => i.Equipo)
            .FirstOrDefaultAsync(i => i.NroImagen == nroImagen && i.NroEquipo == nroEquipo);

        if (imagen is null)
        {
            return Fallo("ImagenNoEncontrada", "No se encontró la foto.");
        }

        if (!EstadoPermiteEditar(imagen.Equipo.EstadoEquipo))
        {
            return Fallo("EquipoNoEditable", MensajeNoEditable(imagen.Equipo.EstadoEquipo));
        }

        var nuevo = string.IsNullOrWhiteSpace(angulo) ? null : angulo.Trim();

        if (nuevo == imagen.Angulo)
        {
            return IdentityResult.Success;
        }

        if (!AngulosFoto.EsValido(nuevo))
        {
            return Fallo("AnguloInvalido", "El ángulo seleccionado no es válido.");
        }

        imagen.Angulo = AngulosFoto.Normalizar(nuevo);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "No se pudo actualizar el ángulo de la imagen {NroImagen}", nroImagen);

            return Fallo("ErrorAlGuardar", "No se pudo actualizar el ángulo. Intentá nuevamente.");
        }

        await AuditarAsync(codigoTecnico, nroEquipo, $"Cambio de ángulo de una imagen del equipo {nroEquipo}");

        return IdentityResult.Success;
    }

    private async Task<IdentityError?> ValidarAsync(ArchivoImagen item)
    {
        var archivo = item.Archivo;
        var nombre = Path.GetFileName(archivo.FileName);

        if (archivo.Length == 0)
        {
            return Error("ArchivoVacio", $"El archivo {nombre} está vacío.");
        }

        if (archivo.Length > _opciones.TamanoMaximoBytes)
        {
            return Error("ArchivoMuyGrande", $"La imagen {nombre} supera el tamaño máximo de {_opciones.TamanoMaximoMB} MB.");
        }

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

        if (extension is not (".jpg" or ".jpeg" or ".png"))
        {
            return Error("FormatoInvalido", $"{nombre}: solo se aceptan imágenes JPG o PNG.");
        }

        if (!await CoincideConFirmaAsync(archivo, extension == ".png" ? FirmaPng : FirmaJpg))
        {
            return Error("FormatoInvalido", $"{nombre} no es una imagen JPG o PNG válida.");
        }

        if (!AngulosFoto.EsValido(item.Angulo))
        {
            return Error("AnguloInvalido", "El ángulo seleccionado no es válido.");
        }

        return null;
    }

    private static async Task<bool> CoincideConFirmaAsync(IFormFile archivo, byte[] firma)
    {
        var encabezado = new byte[firma.Length];

        await using var flujo = archivo.OpenReadStream();
        var leidos = await flujo.ReadAtLeastAsync(encabezado, encabezado.Length, throwOnEndOfStream: false);

        return leidos == firma.Length && encabezado.AsSpan().SequenceEqual(firma);
    }

    private void EliminarArchivos(IEnumerable<string> rutas)
    {
        foreach (var ruta in rutas)
        {
            try
            {
                if (File.Exists(ruta))
                {
                    File.Delete(ruta);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "No se pudo eliminar el archivo {Ruta}", ruta);
            }
        }
    }

    private async Task AuditarAsync(int codigoTecnico, int nroEquipo, string detalle)
    {
        try
        {
            await _auditoriaService.RegistrarEventoAsync(
                TipoOperacionAuditoria.ImagenEquipo, codigoTecnico, nameof(Equipo), nroEquipo, detalle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo auditar el evento de imágenes del equipo {NroEquipo}", nroEquipo);
        }
    }

    private static string MensajeNoEditable(EstadoEquipo estado) => estado switch
    {
        EstadoEquipo.Cancelado => "La donación fue cancelada. No se pueden modificar las fotos del equipo.",
        EstadoEquipo.Baja => "El equipo fue dado de baja. No se pueden modificar sus fotos.",
        EstadoEquipo.Asignado => "El equipo está asignado. No se pueden modificar sus fotos.",
        _ => "El equipo ya fue entregado. No se pueden modificar sus fotos."
    };

    private static ResultadoLoteImagenes Rechazo(string codigo, string descripcion) =>
        ResultadoLoteImagenes.Fallido(Error(codigo, descripcion));

    private static IdentityError Error(string codigo, string descripcion) =>
        new() { Code = codigo, Description = descripcion };

    private static IdentityResult Fallo(string codigo, string descripcion) =>
        IdentityResult.Failed(Error(codigo, descripcion));
}