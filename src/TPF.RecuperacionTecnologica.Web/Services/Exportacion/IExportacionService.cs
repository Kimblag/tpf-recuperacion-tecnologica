using TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

namespace TPF.RecuperacionTecnologica.Web.Services.Exportacion;

public record ArchivoExportado(byte[] Contenido, string TipoContenido, string NombreArchivo);

public interface IExportacionService
{
    Task<ArchivoExportado> ExportarAsync(TablaExportable tabla, FormatoExportacion formato);
}