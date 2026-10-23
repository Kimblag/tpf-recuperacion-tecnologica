using Microsoft.AspNetCore.Identity;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.Services.Equipos;

public interface IImagenEquipoService
{
    /// <summary>
    /// Guarda una imagen del equipo para que se muestre en el catálogo público.
    /// Solo se aceptan archivos JPG o PNG de hasta 5 MB cuyo contenido coincida con su extensión.
    /// </summary>
    /// <returns>
    /// Éxito, o el motivo por el que no se pudo cargar: el equipo no existe o está cancelado,
    /// el formato no es válido o el archivo supera el tamaño máximo.
    /// </returns>
    Task<IdentityResult> CargarAsync(CargaImagenViewModel modelo);
}
