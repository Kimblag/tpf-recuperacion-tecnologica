using System.ComponentModel.DataAnnotations;
using TPF.RecuperacionTecnologica.Web.Models.Enums;
using TPF.RecuperacionTecnologica.Web.ViewModels.Equipos;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Diagnosticos;

public class DiagnosticoFormularioViewModel
{
    public int NroEquipo { get; set; }

    [Display(Name = "Dictamen técnico")]
    [Required(ErrorMessage = "El dictamen técnico es obligatorio.")]
    [EnumDataType(typeof(DictamenTecnico), ErrorMessage = "Seleccione un dictamen válido.")]
    public DictamenTecnico? DictamenTecnico { get; set; }

    [Display(Name = "Fallas detectadas")]
    [StringLength(500, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? DetalleFallas { get; set; }

    [Display(Name = "Detalle de reparación")]
    [StringLength(500, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? DetalleReparacionesRealizadas { get; set; }

    [StringLength(500, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Observaciones { get; set; }

    [StringLength(100, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Procesador { get; set; }

    [Display(Name = "Memoria RAM")]
    [StringLength(30, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? MemoriaRam { get; set; }

    [StringLength(30, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Almacenamiento { get; set; }

    [Display(Name = "Tamaño de pantalla")]
    [StringLength(30, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? TamanoPantalla { get; set; }

    [Display(Name = "Estado de la batería")]
    [StringLength(100, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? EstadoBateria { get; set; }

    [Display(Name = "Otros datos")]
    [StringLength(300, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Otros { get; set; }

    public TipoDiagnostico TipoDiagnostico { get; set; }

    public TipoEquipo TipoEquipo { get; set; }

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public EstadoInicialDeclarado EstadoInicialDeclarado { get; set; }

    public EstadoEquipo EstadoEquipo { get; set; }

    public string? ObservacionesDonante { get; set; }

    public string? ProcesadorDeclarado { get; set; }

    public string? MemoriaRamDeclarada { get; set; }

    public string? AlmacenamientoDeclarado { get; set; }

    public string? TamanoPantallaDeclarado { get; set; }

    public string? EstadoBateriaDeclarado { get; set; }

    public string? OtrosDeclarado { get; set; }

    public DiagnosticoFilaViewModel? DiagnosticoVigente { get; set; }

    public string CodigoEquipo => NroEquipo.ToString("D6");

    public string MarcaYModelo => $"{Marca} {Modelo}".Trim();

    public string Accion => TipoDiagnostico switch
    {
        TipoDiagnostico.Segundo => "Segundo",
        TipoDiagnostico.Rectificacion => "Rectificar",
        _ => "Registrar"
    };

    public string Titulo => TipoDiagnostico switch
    {
        TipoDiagnostico.Segundo => "Segundo diagnóstico",
        TipoDiagnostico.Rectificacion => "Rectificar diagnóstico",
        _ => "Registrar diagnóstico"
    };

    public string TextoGuardar => TipoDiagnostico switch
    {
        TipoDiagnostico.Segundo => "Registrar segundo diagnóstico",
        TipoDiagnostico.Rectificacion => "Guardar rectificación",
        _ => "Registrar diagnóstico"
    };

    public bool RequiereReparaciones => TipoDiagnostico == TipoDiagnostico.Segundo;
}
