using System.ComponentModel.DataAnnotations;

namespace TPF.RecuperacionTecnologica.Web.Models.Enums;

public enum TipoOperacionAuditoria
{
    [Display(Name = "Alta de usuario")]
    AltaUsuario,

    [Display(Name = "Baja de usuario")]
    BajaUsuario,

    [Display(Name = "Donación")]
    Donacion,

    [Display(Name = "Diagnóstico")]
    Diagnostico,

    Solicitud,

    [Display(Name = "Asignación")]
    Asignacion,

    [Display(Name = "Liberación")]
    Liberacion,

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
    BloqueoCuenta
}
