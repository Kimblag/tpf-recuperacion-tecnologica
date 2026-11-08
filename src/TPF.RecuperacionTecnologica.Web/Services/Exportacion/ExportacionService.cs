using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TPF.RecuperacionTecnologica.Web.Services.Configuracion;
using TPF.RecuperacionTecnologica.Web.Services.Reloj;
using TPF.RecuperacionTecnologica.Web.ViewModels.Reportes;

namespace TPF.RecuperacionTecnologica.Web.Services.Exportacion;

public class ExportacionService : IExportacionService
{
    private const string NombreOrganizacionPorDefecto = "Recuperación Tecnológica";
    private const string ColorMarca = "#557744";
    private const string ColorTexto = "#202426";
    private const string ColorSecundario = "#6B7478";
    private const string ColorBorde = "#D8DDDF";
    private const string TipoExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string TipoPdf = "application/pdf";

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-AR");

    private readonly IConfiguracionService _configuracionService;
    private readonly IRelojInstitucional _reloj;

    static ExportacionService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ExportacionService(IConfiguracionService configuracionService, IRelojInstitucional reloj)
    {
        _configuracionService = configuracionService;
        _reloj = reloj;
    }

    public async Task<ArchivoExportado> ExportarAsync(TablaExportable tabla, FormatoExportacion formato)
    {
        var nombre = (await _configuracionService.ObtenerAsync())?.NombreOrganizacion;
        var organizacion = string.IsNullOrWhiteSpace(nombre) ? NombreOrganizacionPorDefecto : nombre.Trim();
        var emision = $"Emitido el {_reloj.FechaHora(_reloj.AhoraUtc)}";
        var fecha = _reloj.Hoy.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return formato == FormatoExportacion.Pdf
            ? new ArchivoExportado(GenerarPdf(tabla, organizacion, emision), TipoPdf, $"{tabla.NombreArchivo}-{fecha}.pdf")
            : new ArchivoExportado(GenerarExcel(tabla, organizacion, emision), TipoExcel, $"{tabla.NombreArchivo}-{fecha}.xlsx");
    }

    private static byte[] GenerarExcel(TablaExportable tabla, string organizacion, string emision)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(tabla.Titulo.Length > 31 ? tabla.Titulo[..31] : tabla.Titulo);

        hoja.Cell(1, 1).Value = organizacion;
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontSize = 11;
        hoja.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml(ColorMarca);

        hoja.Cell(2, 1).Value = tabla.Titulo;
        hoja.Cell(2, 1).Style.Font.Bold = true;
        hoja.Cell(2, 1).Style.Font.FontSize = 16;

        hoja.Cell(3, 1).Value = tabla.Periodo;
        hoja.Cell(4, 1).Value = emision;
        hoja.Range(3, 1, 4, 1).Style.Font.FontColor = XLColor.FromHtml(ColorSecundario);

        const int filaEncabezado = 6;

        for (var c = 0; c < tabla.Columnas.Count; c++)
        {
            var celda = hoja.Cell(filaEncabezado, c + 1);
            celda.Value = tabla.Columnas[c].Titulo;
            celda.Style.Font.Bold = true;
            celda.Style.Font.FontColor = XLColor.White;
            celda.Style.Fill.BackgroundColor = XLColor.FromHtml(ColorMarca);
            celda.Style.Alignment.Horizontal = tabla.Columnas[c].Tipo == TipoColumna.Texto
                ? XLAlignmentHorizontalValues.Left
                : XLAlignmentHorizontalValues.Right;
        }

        var fila = filaEncabezado + 1;

        foreach (var datos in tabla.Filas)
        {
            EscribirFila(hoja, fila, tabla.Columnas, datos, negrita: false);
            fila++;
        }

        if (tabla.Totales is not null)
        {
            EscribirFila(hoja, fila, tabla.Columnas, tabla.Totales, negrita: true);
            hoja.Range(fila, 1, fila, tabla.Columnas.Count).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            fila++;
        }

        var cuerpo = hoja.Range(filaEncabezado, 1, fila - 1, tabla.Columnas.Count);
        cuerpo.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        cuerpo.Style.Border.BottomBorderColor = XLColor.FromHtml(ColorBorde);

        hoja.Columns(1, tabla.Columnas.Count).AdjustToContents();
        hoja.Column(1).Width = Math.Max(hoja.Column(1).Width, 28);
        hoja.SheetView.FreezeRows(filaEncabezado);
        hoja.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        hoja.PageSetup.FitToPages(1, 0);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);

        return memoria.ToArray();
    }

    private static void EscribirFila(
        IXLWorksheet hoja, int fila, IReadOnlyList<ColumnaExportable> columnas, IReadOnlyList<object> datos, bool negrita)
    {
        for (var c = 0; c < columnas.Count && c < datos.Count; c++)
        {
            var celda = hoja.Cell(fila, c + 1);

            switch (columnas[c].Tipo)
            {
                case TipoColumna.Entero:
                    celda.Value = Convert.ToInt32(datos[c], CultureInfo.InvariantCulture);
                    celda.Style.NumberFormat.Format = "#,##0";
                    celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    break;

                case TipoColumna.Porcentaje:
                    celda.Value = Convert.ToDouble(datos[c], CultureInfo.InvariantCulture);
                    celda.Style.NumberFormat.Format = "0.0%";
                    celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    break;

                default:
                    celda.Value = Convert.ToString(datos[c], CultureInfo.InvariantCulture) ?? string.Empty;
                    break;
            }

            celda.Style.Font.Bold = negrita;
        }
    }

    private static byte[] GenerarPdf(TablaExportable tabla, string organizacion, string emision)
    {
        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(40);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(10).FontColor(ColorTexto));

                pagina.Header().PaddingBottom(14).Column(columna =>
                {
                    columna.Item().Text(organizacion).SemiBold().FontSize(11).FontColor(ColorMarca);
                    columna.Item().PaddingTop(2).Text(tabla.Titulo).Bold().FontSize(20);

                    if (!string.IsNullOrWhiteSpace(tabla.Descripcion))
                    {
                        columna.Item().PaddingTop(2).Text(tabla.Descripcion).FontColor(ColorSecundario);
                    }

                    columna.Item().Text(tabla.Periodo).FontColor(ColorSecundario);
                    columna.Item().PaddingTop(8).LineHorizontal(1).LineColor(ColorMarca);
                });

                pagina.Content().Table(grilla =>
                {
                    grilla.ColumnsDefinition(columnas =>
                    {
                        for (var c = 0; c < tabla.Columnas.Count; c++)
                        {
                            columnas.RelativeColumn(c == 0 ? 3f : 1.5f);
                        }
                    });

                    grilla.Header(encabezado =>
                    {
                        foreach (var columna in tabla.Columnas)
                        {
                            var celda = encabezado.Cell().Background(ColorMarca).PaddingVertical(6).PaddingHorizontal(8);
                            AlinearCelda(celda, columna.Tipo).Text(columna.Titulo).Bold().FontColor(Colors.White);
                        }
                    });

                    foreach (var datos in tabla.Filas)
                    {
                        EscribirFilaPdf(grilla, tabla.Columnas, datos, negrita: false);
                    }

                    if (tabla.Totales is not null)
                    {
                        EscribirFilaPdf(grilla, tabla.Columnas, tabla.Totales, negrita: true);
                    }
                });

                pagina.Footer().Row(fila =>
                {
                    fila.RelativeItem().Text(emision).FontSize(8).FontColor(ColorSecundario);
                    fila.RelativeItem().AlignRight().Text(texto =>
                    {
                        texto.DefaultTextStyle(estilo => estilo.FontSize(8).FontColor(ColorSecundario));
                        texto.Span("Página ");
                        texto.CurrentPageNumber();
                        texto.Span(" de ");
                        texto.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void EscribirFilaPdf(
        TableDescriptor grilla, IReadOnlyList<ColumnaExportable> columnas, IReadOnlyList<object> datos, bool negrita)
    {
        for (var c = 0; c < columnas.Count && c < datos.Count; c++)
        {
            var celda = grilla.Cell()
                .BorderBottom(negrita ? 1 : 0.5f)
                .BorderColor(negrita ? ColorTexto : ColorBorde)
                .PaddingVertical(6)
                .PaddingHorizontal(8);

            var texto = AlinearCelda(celda, columnas[c].Tipo).Text(Formatear(datos[c], columnas[c].Tipo));

            if (negrita)
            {
                texto.Bold();
            }
        }
    }

    private static IContainer AlinearCelda(IContainer celda, TipoColumna tipo) =>
        tipo == TipoColumna.Texto ? celda : celda.AlignRight();

    private static string Formatear(object valor, TipoColumna tipo) => tipo switch
    {
        TipoColumna.Entero => Convert.ToInt32(valor, CultureInfo.InvariantCulture).ToString("N0", Cultura),
        TipoColumna.Porcentaje => Convert.ToDouble(valor, CultureInfo.InvariantCulture).ToString("P1", Cultura),
        _ => Convert.ToString(valor, CultureInfo.InvariantCulture) ?? string.Empty
    };
}