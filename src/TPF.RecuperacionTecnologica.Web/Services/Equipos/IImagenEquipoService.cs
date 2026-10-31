using Microsoft.AspNetCore.Identity;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.Services.Equipos;

public interface IImagenEquipoService
{
    Task<GestionFotosViewModel?> ObtenerGestionAsync(int nroEquipo);

    Task<ResultadoLoteImagenes> PrepararAsync(int nroEquipo, IReadOnlyList<ArchivoImagen> archivos);

    void DescartarArchivos(ResultadoLoteImagenes lote);

    Task<IdentityResult> AgregarAsync(int nroEquipo, IReadOnlyList<ArchivoImagen> archivos, int codigoTecnico);

    Task<IdentityResult> EliminarAsync(int nroEquipo, int nroImagen, int codigoTecnico);

    Task<IdentityResult> ActualizarAnguloAsync(int nroEquipo, int nroImagen, string? angulo, int codigoTecnico);
}