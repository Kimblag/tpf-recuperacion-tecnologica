using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPF.RecuperacionTecnologica.Web.Models;

namespace TPF.RecuperacionTecnologica.Web.Data.Configurations;

public class SolicitudConfiguration : IEntityTypeConfiguration<Solicitud>
{
    public void Configure(EntityTypeBuilder<Solicitud> builder)
    {
        builder.HasKey(s => s.NroSolicitud);

        builder.HasOne(s => s.Usuario)
            .WithMany()
            .HasForeignKey(s => s.NroUsuario)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.EquipoRequerido)
            .WithMany()
            .HasForeignKey(s => s.NroEquipoRequerido)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Coordinador)
            .WithMany()
            .HasForeignKey(s => s.CodigoCoordinador)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.MotivoSolicitud)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.MotivoCancelacion)
            .HasMaxLength(300);

        builder.Property(s => s.FechaSolicitud)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(s => s.RowVersion)
            .IsRowVersion();
    }
}
