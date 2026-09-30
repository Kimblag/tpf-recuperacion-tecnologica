using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPF.RecuperacionTecnologica.Web.Models;

namespace TPF.RecuperacionTecnologica.Web.Data.Configurations;

public class DiagnosticoConfiguration : IEntityTypeConfiguration<Diagnostico>
{
    public void Configure(EntityTypeBuilder<Diagnostico> builder)
    {
        builder.HasKey(d => d.NroDiagnostico);

        builder.HasOne(d => d.Equipo)
            .WithMany()
            .HasForeignKey(d => d.NroEquipo)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Tecnico)
            .WithMany()
            .HasForeignKey(d => d.CodigoTecnico)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DiagnosticoRectificado)
            .WithMany()
            .HasForeignKey(d => d.DiagnosticoRectificadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(d => d.DetalleFallas)
            .HasMaxLength(500);

        builder.Property(d => d.DetalleReparacionesRealizadas)
            .HasMaxLength(500);

        builder.Property(d => d.Observaciones)
            .HasMaxLength(500);

        builder.ComplexProperty(d => d.EspecificacionesVerificadas, especificaciones =>
        {
            especificaciones.Property(x => x.Otros)
                .HasMaxLength(300);
        });
    }
}
