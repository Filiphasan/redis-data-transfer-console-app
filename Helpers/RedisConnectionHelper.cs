using RedisKeyMover.Models;
using RedisKeyMover.Redis;

namespace RedisKeyMover.Helpers;

public sealed record RedisConfigRecord(string Host, int Port, string Password, int Database)
{
    public string ConnectionString => $"{Host}:{Port},password={Password}";
}

public static class RedisConnectionHelper
{
    private static RedisConfigRecord ReadConfigFromOperation(RedisOperationModel operationModel, bool isSource)
    {
        var host = isSource ? operationModel.Operation.SourceHost : operationModel.Operation.TargetHost;
        var port = isSource ? operationModel.Operation.SourcePort : operationModel.Operation.TargetPort;
        var password = isSource ? operationModel.Operation.SourcePassword : operationModel.Operation.TargetPassword;
        var dbNumber = isSource ? operationModel.Operation.SourceDatabase : operationModel.Operation.TargetDatabase;
        return new RedisConfigRecord(host, port, password, dbNumber);
    }
    
    public static RedisConfigRecord ReadConfig(RedisOperationModel? operationModel, bool isSource)
    {
        var key = isSource ? "Kaynak" : "Hedef";

        if (operationModel is not null)
        {
            return ReadConfigFromOperation(operationModel, isSource);
        }

        var host = ConsoleHelper.ReadLine($"{key} Redis Host: ");
        string? portInput = ConsoleHelper.ReadLine($"{key} Redis Port: ");
        var password = ConsoleHelper.ReadLine($"{key} Redis Password: ");
        string? dbInput = ConsoleHelper.ReadLine($"{key} Redis Database No: ");

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException($"{key} Redis Host bilgisi gerekli!");
        }

        if (!int.TryParse(portInput, out var port))
        {
            throw new ArgumentException($"{key} Redis Port bilgisi gerekli!");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException($"{key} Redis Password bilgisi gerekli!");
        }

        if (!int.TryParse(dbInput, out var dbNumber) && dbNumber is >= 0 and < 16)
        {
            dbNumber = 0;
        }

        return new RedisConfigRecord(host, port, password, dbNumber);
    }
    
    public static async Task<bool> TestConnectionsAsync(
        RedisService sourceRedis,
        RedisService targetRedis
    )
    {
        try
        {
            if (await sourceRedis.ConnectAsync())
            {
                ConsoleHelper.WriteSuccess("✓ Kaynak Redis bağlantısı başarılı");
            }
            else
            {
                ConsoleHelper.WriteError("✗ Kaynak Redis bağlantısı başarısız!");
                return false;
            }

            if (await targetRedis.ConnectAsync())
            {
                ConsoleHelper.WriteSuccess("✓ Hedef Redis bağlantısı başarılı");
            }
            else
            {
                ConsoleHelper.WriteError("✗ Hedef Redis bağlantısı başarısız!");
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Bağlantı hatası: {ex.Message}");
            return false;
        }
    }
}
