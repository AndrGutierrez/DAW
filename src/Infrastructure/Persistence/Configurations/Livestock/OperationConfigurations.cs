using Core.Domain.Livestock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Livestock;

public sealed class AnimalMovementConfiguration : IEntityTypeConfiguration<AnimalMovement>
{
    public void Configure(EntityTypeBuilder<AnimalMovement> builder)
    {
        builder.ToTable("AnimalMovements");
        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.Reason).HasMaxLength(300);

        builder.HasIndex(movement => new { movement.AnimalId, movement.Date });

        builder.HasOne(movement => movement.Animal)
            .WithMany()
            .HasForeignKey(movement => movement.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(movement => movement.FromPaddock)
            .WithMany()
            .HasForeignKey(movement => movement.FromPaddockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(movement => movement.ToPaddock)
            .WithMany()
            .HasForeignKey(movement => movement.ToPaddockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(movement => movement.FromLot)
            .WithMany()
            .HasForeignKey(movement => movement.FromLotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(movement => movement.ToLot)
            .WithMany()
            .HasForeignKey(movement => movement.ToLotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(task => task.Id);

        builder.Property(task => task.Title).IsRequired().HasMaxLength(200);
        builder.Property(task => task.Description).HasMaxLength(2000);
        builder.Property(task => task.Type).HasConversion<int>().IsRequired();
        builder.Property(task => task.Priority).HasConversion<int>().IsRequired();
        builder.Property(task => task.Status).HasConversion<int>().IsRequired();

        builder.HasIndex(task => new { task.FarmId, task.Status, task.DueDate });

        builder.HasOne(task => task.RelatedAnimal)
            .WithMany()
            .HasForeignKey(task => task.RelatedAnimalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(task => task.RelatedLot)
            .WithMany()
            .HasForeignKey(task => task.RelatedLotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("AlertRules");
        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Type).HasConversion<int>().IsRequired();
        builder.Property(rule => rule.ThresholdValue).HasPrecision(12, 2);
        builder.Property(rule => rule.IsEnabled).IsRequired();

        builder.HasIndex(rule => new { rule.FarmId, rule.Type });
    }
}

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");
        builder.HasKey(alert => alert.Id);

        builder.Property(alert => alert.Type).HasConversion<int>().IsRequired();
        builder.Property(alert => alert.Severity).HasConversion<int>().IsRequired();
        builder.Property(alert => alert.Message).IsRequired().HasMaxLength(500);
        builder.Property(alert => alert.RelatedEntityType).HasMaxLength(100);
        builder.Property(alert => alert.IsResolved).IsRequired();

        builder.HasIndex(alert => new { alert.FarmId, alert.IsResolved, alert.Severity });
    }
}

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Type).HasConversion<int>().IsRequired();
        builder.Property(transaction => transaction.Category).HasConversion<int>().IsRequired();
        builder.Property(transaction => transaction.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(transaction => transaction.Currency).IsRequired().HasMaxLength(3);
        builder.Property(transaction => transaction.Description).HasMaxLength(500);
        builder.Property(transaction => transaction.Counterparty).HasMaxLength(200);

        builder.HasIndex(transaction => new { transaction.FarmId, transaction.Date });

        builder.HasOne(transaction => transaction.Animal)
            .WithMany()
            .HasForeignKey(transaction => transaction.AnimalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(transaction => transaction.Lot)
            .WithMany()
            .HasForeignKey(transaction => transaction.LotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(transaction => transaction.Product)
            .WithMany()
            .HasForeignKey(transaction => transaction.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");
        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.OwnerType).IsRequired().HasMaxLength(100);
        builder.Property(attachment => attachment.FileName).IsRequired().HasMaxLength(255);
        builder.Property(attachment => attachment.Url).IsRequired().HasMaxLength(500);
        builder.Property(attachment => attachment.ContentType).HasMaxLength(100);

        builder.HasIndex(attachment => new { attachment.OwnerType, attachment.OwnerId });
    }
}

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.Action).IsRequired().HasMaxLength(50);
        builder.Property(log => log.EntityName).IsRequired().HasMaxLength(150);
        builder.Property(log => log.EntityId).HasMaxLength(100);
        builder.Property(log => log.OldValues).HasColumnType("jsonb");
        builder.Property(log => log.NewValues).HasColumnType("jsonb");
        builder.Property(log => log.IpAddress).HasMaxLength(64);
        builder.Property(log => log.OccurredAt).IsRequired();

        builder.HasIndex(log => new { log.EntityName, log.EntityId });
    }
}
