using StackExchange.Redis;

namespace RedisKeyMover.Redis;

public sealed class RedisService(string connectionString) : IAsyncDisposable
{
    private ConnectionMultiplexer? _connection;
    private readonly string _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    private bool _disposed;

    public async Task<bool> ConnectAsync()
    {
        try
        {
            if (_connection is not { IsConnected: true })
            {
                if (_connection != null)
                {
                    await _connection.DisposeAsync();
                }
                _connection = await ConnectionMultiplexer.ConnectAsync(_connectionString);
            }

            return _connection.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    public IDatabase GetDatabase(int database = -1)
    {
        if (_connection is not { IsConnected: true })
        {
            throw new InvalidOperationException("Redis bağlantısı kurulmamış. Önce Connect() veya ConnectAsync() metodunu çağırın.");
        }
        return _connection.GetDatabase(database);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _disposed = true;
        _connection = null;
    }
}