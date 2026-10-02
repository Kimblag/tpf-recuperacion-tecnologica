using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TPF.RecuperacionTecnologica.Web.Models;

namespace TPF.RecuperacionTecnologica.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Personal> PersonalInterno => Set<Personal>();
        public DbSet<Equipo> Equipos => Set<Equipo>();
        public DbSet<ImagenEquipo> ImagenEquipos => Set<ImagenEquipo>();
        public DbSet<Diagnostico> Diagnosticos => Set<Diagnostico>();
        public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
        public DbSet<Asignacion> Asignaciones => Set<Asignacion>();
        public DbSet<Entrega> Entregas => Set<Entrega>();
        public DbSet<ConfiguracionInstitucional> ConfiguracionInstitucional => Set<ConfiguracionInstitucional>();
        public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // permite que configuremos las tablas de Identity
            base.OnModelCreating(builder);

            // busca en el assembly las clases de tipo IEntityTypeConfiguration y las aplica.
            builder.ApplyConfigurationsFromAssembly(
                typeof(ApplicationDbContext).Assembly);
        }
    }
}
