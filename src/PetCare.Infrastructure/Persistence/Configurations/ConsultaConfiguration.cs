using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetCare.Domain.Entities;

namespace PetCare.Infrastructure.Persistence.Configurations;

public class ConsultaConfiguration : IEntityTypeConfiguration<Consulta>
{
    public void Configure(EntityTypeBuilder<Consulta> builder)
    {
        builder.ToTable("CONSULTA");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("ID_CONSULTA");

        builder.Property(c => c.DataConsulta)
            .HasColumnName("DATA_CONSULTA")
            .IsRequired();

        builder.Property(c => c.Observacoes)
            .HasColumnName("OBSERVACOES")
            .HasMaxLength(1000);

        builder.Property(c => c.PetId)
            .HasColumnName("ID_PET")
            .IsRequired();
    }
}
