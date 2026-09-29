using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPF.RecuperacionTecnologica.Web.Models;

namespace TPF.RecuperacionTecnologica.Web.Data.Configurations;

public class EquipoConfiguration : IEntityTypeConfiguration<Equipo>
{
    public void Configure(EntityTypeBuilder<Equipo> builder)
    {
        builder.HasKey(e => e.NroEquipo);

        builder.HasOne(e => e.UsuarioDonante)
            .WithMany()
            .HasForeignKey(e => e.NroUsuarioDonante)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Marca)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(e => e.Modelo)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(e => e.Observaciones)
            .HasMaxLength(500);

        builder.ComplexProperty(e => e.EspecificacionesDeclaradas, especificaciones =>
        {
            especificaciones.Property(x => x.Otros)
                .HasMaxLength(300);
        });

        builder.Property(e => e.MotivoCancelacion)
            .HasMaxLength(300);

        builder.Property(e => e.ImagenUrl)
            .HasMaxLength(500);

        builder.Property(e => e.RowVersion)
            .IsRowVersion();
    }
}
