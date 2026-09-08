using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetCare.Domain.Entities;

namespace PetCare.Infrastructure.Persistence.Configurations;

public class TutorConfiguration : IEntityTypeConfiguration<Tutor>
{
    public void Configure(EntityTypeBuilder<Tutor> builder)
    {
        builder.ToTable("TUTOR");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("ID_TUTOR");

        builder.Property(t => t.Nome)
            .HasColumnName("NOME")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.Email)
            .HasColumnName("EMAIL")
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.Telefone)
            .HasColumnName("TELEFONE")
            .HasMaxLength(20);

        builder.HasIndex(t => t.Email)
            .IsUnique()
            .HasDatabaseName("UX_TUTOR_EMAIL");

        builder.HasMany(t => t.Pets)
            .WithOne(p => p.Tutor)
            .HasForeignKey(p => p.TutorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
