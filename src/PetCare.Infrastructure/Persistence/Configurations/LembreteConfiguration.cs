using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetCare.Domain.Entities;

namespace PetCare.Infrastructure.Persistence.Configurations;

public class LembreteConfiguration : IEntityTypeConfiguration<Lembrete>
{
    public void Configure(EntityTypeBuilder<Lembrete> builder)
    {
        builder.ToTable("LEMBRETE");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("ID_LEMBRETE");

        builder.Property(l => l.Titulo)
            .HasColumnName("TITULO")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(l => l.Descricao)
            .HasColumnName("DESCRICAO")
            .HasMaxLength(500);

        builder.Property(l => l.DataLembrete)
            .HasColumnName("DATA_LEMBRETE")
            .IsRequired();

        builder.Property(l => l.PetId)
            .HasColumnName("ID_PET")
            .IsRequired();
    }
}
