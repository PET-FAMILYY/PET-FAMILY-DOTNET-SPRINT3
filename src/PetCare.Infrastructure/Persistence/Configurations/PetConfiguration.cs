using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetCare.Domain.Entities;

namespace PetCare.Infrastructure.Persistence.Configurations;

public class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("PET");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("ID_PET");

        builder.Property(p => p.Nome)
            .HasColumnName("NOME")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Especie)
            .HasColumnName("ESPECIE")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Raca)
            .HasColumnName("RACA")
            .HasMaxLength(50);

        builder.Property(p => p.DataNascimento)
            .HasColumnName("DATA_NASCIMENTO")
            .IsRequired();

        builder.Property(p => p.TutorId)
            .HasColumnName("ID_TUTOR")
            .IsRequired();

        builder.HasMany(p => p.Consultas)
            .WithOne(c => c.Pet)
            .HasForeignKey(c => c.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Lembretes)
            .WithOne(l => l.Pet)
            .HasForeignKey(l => l.PetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
