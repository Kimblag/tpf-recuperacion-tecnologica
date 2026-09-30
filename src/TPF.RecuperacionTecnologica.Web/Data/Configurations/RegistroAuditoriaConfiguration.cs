using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TPF.RecuperacionTecnologica.Web.Models;

namespace TPF.RecuperacionTecnologica.Web.Data.Configurations;

public class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.HasKey(r => r.NroAuditoria);

        builder.HasOne(r => r.Personal)
            .WithMany()
            .HasForeignKey(r => r.CodigoPersonal)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.EntidadAfectada)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(r => r.Detalle)
            .HasMaxLength(500);
    }
}
