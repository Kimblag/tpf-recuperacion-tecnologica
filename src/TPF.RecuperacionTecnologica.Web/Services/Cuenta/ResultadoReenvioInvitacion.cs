namespace TPF.RecuperacionTecnologica.Web.Services.Cuenta;

public enum ResultadoReenvioInvitacion
{
    Enviada,

    /// <summary>Ya se envió una invitación a esta cuenta hace menos de 2 minutos.</summary>
    Espera,

    /// <summary>La cuenta ya eligió su contraseña, así que no necesita invitación.</summary>
    YaActiva,

    /// <summary>La cuenta está dada de baja.</summary>
    CuentaInactiva,

    NoExiste
}
