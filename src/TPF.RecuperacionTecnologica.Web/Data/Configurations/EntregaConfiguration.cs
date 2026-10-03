using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPF.RecuperacionTecnologica.Web.Models;

namespace TPF.RecuperacionTecnologica.Web.Data.Configurations;

public class EntregaConfiguration : IEntityTypeConfiguration<Entrega>
{
    public void Configure(EntityTypeBuilder<Entrega> builder)
    {
        builder.HasKey(e => e.NroEntrega);

        builder.HasOne(e => e.Equipo)
            .WithMany()
            .HasForeignKey(e => e.NroEquipo)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Solicitud)
            .WithMany()
            .HasForeignKey(e => e.NroSolicitud)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Asignacion)
            .WithMany()
            .HasForeignKey(e => e.NroAsignacion)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Coordinador)
            .WithMany()
            .HasForeignKey(e => e.CodigoCoordinador)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.DocumentoIdentidadQuienRetira)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.FechaEntrega)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
