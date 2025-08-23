using Microsoft.EntityFrameworkCore;

namespace RedisKeyMover.Data.Contexts;

public class AppDbContextFactory
{
    private static readonly Lazy<AppDbContextFactory> Lazy = new(() => new AppDbContextFactory());
    private readonly DbContextOptions<AppDbContext> _options;

    private AppDbContextFactory()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=rediskeydb.db")
            .Options;
    }

    public static AppDbContextFactory Instance => Lazy.Value;

    public AppDbContext CreateContext()
    {
        var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
        return context;
    }
}
