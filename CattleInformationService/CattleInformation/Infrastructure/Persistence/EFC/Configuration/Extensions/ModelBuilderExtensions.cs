using Microsoft.EntityFrameworkCore;

namespace CattleInformationService.CattleInformation.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyCattleInformationConfiguration(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Model.Aggregates.CattleInformation>(entity =>
        {
            entity.ToTable("cattle_information");

            // Primary Key
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            // Ranch ID
            entity.Property(c => c.RanchId)
                .HasColumnName("ranch_id")
                .IsRequired();

            // Ear Tag
            entity.Property(c => c.EarTag)
                .HasColumnName("ear_tag")
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(c => new { c.RanchId, c.EarTag })
                .IsUnique();

            // Breed
            entity.Property(c => c.Breed)
                .HasColumnName("breed")
                .HasMaxLength(100)
                .IsRequired();

            // Sex
            entity.Property(c => c.Sex)
                .HasColumnName("sex")
                .HasMaxLength(20)
                .IsRequired();

            // Birth Date
            entity.Property(c => c.BirthDate)
                .HasColumnName("birth_date")
                .HasColumnType("date")
                .IsRequired();

            // Status
            entity.Property(c => c.Status)
                .HasColumnName("status")
                .IsRequired();

            // Current Stage
            entity.Property(c => c.CurrentStage)
                .HasColumnName("current_stage")
                .HasMaxLength(50)
                .IsRequired();

            // Sire Ear Tag
            entity.Property(c => c.SireEarTag)
                .HasColumnName("sire_ear_tag")
                .HasMaxLength(50)
                .IsRequired(false);

            // Dam Ear Tag
            entity.Property(c => c.DamEarTag)
                .HasColumnName("dam_ear_tag")
                .HasMaxLength(50)
                .IsRequired(false);

            // Exit Reason
            entity.Property(c => c.ExitReason)
                .HasColumnName("exit_reason")
                .HasMaxLength(255)
                .IsRequired(false);

            // Exit Date
            entity.Property(c => c.ExitDate)
                .HasColumnName("exit_date")
                .HasColumnType("date")
                .IsRequired(false);
        });
    }
}