using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPF.RecuperacionTecnologica.Web.Models;

namespace TPF.RecuperacionTecnologica.Web.Data.Configurations;

public class AsignacionConfiguration : IEntityTypeConfiguration<Asignacion>
{
    public void Configure(EntityTypeBuilder<Asignacion> builder)
    {
        builder.HasKey(a => a.NroAsignacion);

        builder.HasOne(a => a.Equipo)
            .WithMany()
            .HasForeignKey(a => a.NroEquipo)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Solicitud)
            .WithMany()
            .HasForeignKey(a => a.NroSolicitud)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Coordinador)
            .WithMany()
            .HasForeignKey(a => a.CodigoCoordinador)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.JustificacionCambio)
            .HasMaxLength(500);

        builder.Property(a => a.MotivoLiberacion)
            .HasMaxLength(300);

        builder.Property(a => a.FechaAsignacion)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Asignacion_FechaLiberacion_Coherente",
            "[FechaLiberacion] IS NULL OR [FechaLiberacion] >= [FechaAsignacion]"));

        builder.Property(a => a.RowVersion)
            .IsRowVersion();
    }
}
