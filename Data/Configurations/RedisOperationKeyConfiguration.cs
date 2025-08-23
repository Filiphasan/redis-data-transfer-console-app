using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedisKeyMover.Data.Entities;

namespace RedisKeyMover.Data.Configurations;

public class RedisOperationKeyConfiguration : IEntityTypeConfiguration<RedisOperationKey>
{
    public void Configure(EntityTypeBuilder<RedisOperationKey> builder)
    {
        builder.ToTable("RedisOperationKeys");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.OperationId)
            .IsRequired();

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.KeyType)
            .IsRequired();

        builder.Property(x => x.Success)
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);

        // Foreign Key indeks
        builder.HasIndex(x => x.OperationId)
            .HasDatabaseName("IX_RedisOperationKeys_OperationId");

        builder.HasIndex(x => x.Key)
            .HasDatabaseName("IX_RedisOperationKeys_Key");

        // Many-to-One ilişki
        builder.HasOne(x => x.Operation)
            .WithMany(x => x.Keys)
            .HasForeignKey(x => x.OperationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}