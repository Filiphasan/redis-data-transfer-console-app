using Microsoft.EntityFrameworkCore;
using RedisKeyMover.Data.Entities;
using RedisKeyMover.Data.Configurations;

namespace RedisKeyMover.Data.Contexts;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<RedisOperation> RedisOperations { get; set; }
    public DbSet<RedisOperationKey> RedisOperationKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RedisOperationConfiguration());
        modelBuilder.ApplyConfiguration(new RedisOperationKeyConfiguration());
        
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=rediskeydb.db");
        }
    }
}
