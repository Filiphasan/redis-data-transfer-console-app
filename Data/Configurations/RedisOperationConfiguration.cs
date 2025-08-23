using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedisKeyMover.Data.Entities;

namespace RedisKeyMover.Data.Configurations;

public class RedisOperationConfiguration : IEntityTypeConfiguration<RedisOperation>
{
    public void Configure(EntityTypeBuilder<RedisOperation> builder)
    {
        builder.ToTable("RedisOperations");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(x => x.SourceHost)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(x => x.SourcePort)
            .IsRequired();
            
        builder.Property(x => x.SourcePassword)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(x => x.SourceDatabase)
            .IsRequired();
            
        builder.Property(x => x.TargetHost)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(x => x.TargetPort)
            .IsRequired();
            
        builder.Property(x => x.TargetPassword)
            .IsRequired()
            .HasMaxLength(255);
            
        builder.Property(x => x.TargetDatabase)
            .IsRequired();
            
        builder.Property(x => x.Pattern)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(x => x.StartTime)
            .IsRequired();
            
        builder.Property(x => x.EndTime)
            .IsRequired();
            
        builder.Property(x => x.SuccessCount)
            .IsRequired();
            
        builder.Property(x => x.FailCount)
            .IsRequired();
            
        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);
            
        // One-to-Many ilişki
        builder.HasMany(x => x.Keys)
            .WithOne(x => x.Operation)
            .HasForeignKey(x => x.OperationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
