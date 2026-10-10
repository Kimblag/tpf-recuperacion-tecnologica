using TPF.RecuperacionTecnologica.Web.ViewModels.Auditoria;

namespace TPF.RecuperacionTecnologica.Web.ViewModels.Administrador;

public class AdministradorInicioViewModel
{
    public int UsuariosExternos { get; set; }

    public int UsuariosNuevosDelMes { get; set; }

    public int PersonalActivo { get; set; }

    public int PersonalInactivo { get; set; }

    public int Administradores { get; set; }

    public int Coordinadores { get; set; }

    public int Tecnicos { get; set; }

    public int EntregasDelMes { get; set; }

    public int VariacionEntregasPorcentaje { get; set; }

    public IReadOnlyList<EquipoEstadoCantidadViewModel> EquiposPorEstado { get; set; } = [];

    public IReadOnlyList<AuditoriaFilaViewModel> AuditoriaReciente { get; set; } = [];

    public int EquiposRegistrados => EquiposPorEstado.Sum(e => e.Cantidad);
}
