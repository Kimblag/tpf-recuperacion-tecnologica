using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPF.RecuperacionTecnologica.Web.Models;

namespace TPF.RecuperacionTecnologica.Web.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.HasKey(u => u.NroUsuario);

        builder.Property(u => u.IdentityUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(u => u.IdentityUserId)
            .IsUnique();

        builder.Property(u => u.Nombre)
            .HasMaxLength(100);

        builder.Property(u => u.Apellido)
            .HasMaxLength(100);

        builder.Property(u => u.RazonSocial)
            .HasMaxLength(150);

        builder.Property(u => u.NroDocumento)
            .IsRequired()
            .HasMaxLength(11);

        builder.HasIndex(u => u.NroDocumento)
            .IsUnique();

        builder.Property(u => u.Direccion)
            .HasMaxLength(200);

        builder.Property(u => u.Telefono)
            .HasMaxLength(20);

        builder.Property(u => u.CorreoElectronico)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(u => u.MotivoBaja)
            .HasMaxLength(300);

        builder.HasIndex(u => u.CorreoElectronico)
            .IsUnique();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Usuario_NroDocumento_SoloDigitos",
            "[NroDocumento] NOT LIKE '%[^0-9]%'"));
    }
}
