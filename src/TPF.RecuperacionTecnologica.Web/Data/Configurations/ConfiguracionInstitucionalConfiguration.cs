using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPF.RecuperacionTecnologica.Web.Models;

namespace TPF.RecuperacionTecnologica.Web.Data.Configurations;

public class ConfiguracionInstitucionalConfiguration : IEntityTypeConfiguration<ConfiguracionInstitucional>
{
    public void Configure(EntityTypeBuilder<ConfiguracionInstitucional> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.ToTable(t => t.HasCheckConstraint("CK_ConfiguracionInstitucional_Id_Unico", "[Id] = 1"));

        builder.Property(c => c.NombreOrganizacion)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.ZonaCobertura)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(c => c.Administrador)
            .WithMany()
            .HasForeignKey(c => c.CodigoAdministrador)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.RowVersion)
            .IsRowVersion();
    }
}
