using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Data;
using TPF.RecuperacionTecnologica.Web.ViewModels.Compartido;

namespace TPF.RecuperacionTecnologica.Web.Services.Equipos;

public static class ConsultasImagenesEquipo
{
    public static async Task<List<FotoEquipoViewModel>> ObtenerFotosAsync(this ApplicationDbContext context, int nroEquipo) =>
         await context.ImagenesEquipo
            .AsNoTracking()
            .Where(i => i.NroEquipo == nroEquipo)
            .OrderBy(i => i.FechaCarga)
            .ThenBy(i => i.NroImagen)
            .Select(i => new FotoEquipoViewModel(i.UrlImagen, i.Angulo))
            .ToListAsync();
}