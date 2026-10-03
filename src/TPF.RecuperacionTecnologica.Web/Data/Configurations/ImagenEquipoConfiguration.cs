using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPF.RecuperacionTecnologica.Web.Models;

namespace TPF.RecuperacionTecnologica.Web.Data.Configurations;

public class ImagenEquipoConfiguration : IEntityTypeConfiguration<ImagenEquipo>
{
    public void Configure(EntityTypeBuilder<ImagenEquipo> builder)
    {
        builder.HasKey(i => i.NroImagen);

        builder.HasOne(i => i.Equipo)
            .WithMany()
            .HasForeignKey(i => i.NroEquipo)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Diagnostico)
            .WithMany()
            .HasForeignKey(i => i.NroDiagnostico)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.UrlImagen)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.Angulo)
            .HasMaxLength(40);

        builder.Property(i => i.FechaCarga)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
