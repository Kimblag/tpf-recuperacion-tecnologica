using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.Models.Enums;

public enum TipoOperacionAuditoria
{
    [Display(Name = "Alta de usuario")]
    AltaUsuario,

    [Display(Name = "Baja de usuario")]
    BajaUsuario,

    [Display(Name = "Registro de donación")]
    Donacion,

    [Display(Name = "Registro de diagnóstico")]
    Diagnostico,

    [Display(Name = "Registro de solicitud")]
    Solicitud,

    [Display(Name = "Asignación de equipo")]
    Asignacion,

    [Display(Name = "Liberación")]
    Liberacion,

    [Display(Name = "Registro de entrega")]
    Entrega,

    [Display(Name = "Alta de personal")]
    AltaPersonal,

    [Display(Name = "Inactivación de personal")]
    InactivacionPersonal,

    [Display(Name = "Modificación de parámetro")]
    ModificacionParametro,

    [Display(Name = "Inicio de sesión")]
    InicioSesion,

    [Display(Name = "Cierre de sesión")]
    CierreSesion,

    [Display(Name = "Intento fallido de sesión")]
    IntentoFallidoSesion,

    [Display(Name = "Bloqueo de cuenta")]
    BloqueoCuenta,

    [Display(Name = "Edición de personal")]
    EdicionPersonal,

    [Display(Name = "Imagen de equipo")]
    ImagenEquipo,

    [Display(Name = "Activación de cuenta")]
    ActivacionCuenta,

    [Display(Name = "Reactivación de personal")]
    ReactivacionPersonal,

    [Display(Name = "Ingreso sin registro")]
    IngresoSinRegistro,

    [Display(Name = "Corrección de datos de usuario")]
    CorreccionUsuario
}