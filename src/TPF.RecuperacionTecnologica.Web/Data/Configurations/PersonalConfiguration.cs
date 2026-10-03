using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPF.RecuperacionTecnologica.Web.Models;

namespace TPF.RecuperacionTecnologica.Web.Data.Configurations;

public class PersonalConfiguration : IEntityTypeConfiguration<Personal>
{
    public void Configure(EntityTypeBuilder<Personal> builder)
    {
        builder.HasKey(p => p.CodigoPersonal);

        builder.Property(p => p.IdentityUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(p => p.IdentityUserId)
            .IsUnique();

        builder.Property(p => p.Dni)
            .IsRequired()
            .HasMaxLength(8);

        builder.HasIndex(p => p.Dni)
            .IsUnique();

        builder.ToTable(t => t.HasCheckConstraint("CK_Personal_Dni_SoloDigitos", "[Dni] NOT LIKE '%[^0-9]%'"));

        builder.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Apellido)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Telefono)
            .HasMaxLength(20);

        builder.Property(p => p.CorreoElectronico)
            .IsRequired()
            .HasMaxLength(254);

        builder.HasIndex(p => p.CorreoElectronico)
            .IsUnique();

        builder.Property(p => p.MotivoInactivacion)
            .HasMaxLength(300);
        
        builder.Property(p => p.FechaAlta)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
