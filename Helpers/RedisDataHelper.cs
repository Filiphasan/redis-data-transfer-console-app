using RedisKeyMover.Models;
using StackExchange.Redis;

namespace RedisKeyMover.Helpers;

public static class RedisDataHelper
{
    public static SemaphoreSlim Semaphore = new(10, 10);
    
    public static async Task<List<RedisKeyOperationModel>> ReadAllKeysAsync(this IDatabase database, string pattern = "*")
    {
        var keys = new List<RedisKeyOperationModel>();
        var server = database.Multiplexer.GetServer(database.Multiplexer.GetEndPoints().First(ep => !database.Multiplexer.GetServer(ep).IsReplica));

        await foreach (var key in server.KeysAsync(database.Database, pattern, pageSize: 1000))
        {
            keys.Add(new RedisKeyOperationModel { Key = key.ToString() });
        }

        return keys;
    }

    public static async Task TransferKeysAsync(this IDatabase sourceDatabase, IDatabase targetDatabase, List<RedisKeyOperationModel> keys, bool deleteSourceKeys = false)
    {
        if (keys.Count == 0)
        {
            return;
        }

        var readTasks = keys.Select(x => ReadKeyDataAsync(sourceDatabase, x));
        await Task.WhenAll(readTasks);

        var writeTasks = keys.Where(x => x.Success).Select(x => WriteKeyDataAsync(targetDatabase, x));
        await Task.WhenAll(writeTasks);

        if (deleteSourceKeys && keys.Exists(x => x.Success))
        {
            await DeleteSourceKeysAsync(sourceDatabase, keys.Where(x => x.Success));
        }
    }

    private static async Task ReadKeyDataAsync(IDatabase database, RedisKeyOperationModel operation)
    {
        await Semaphore.WaitAsync();
        try
        {
            
            var keyType = await database.KeyTypeAsync(operation.Key);
            var ttl = await database.KeyTimeToLiveAsync(operation.Key);

            operation.KeyType = keyType;
            operation.Ttl = ttl;

            if (keyType == RedisType.None)
            {
                operation.Success = true;
                return;
            }

            // Key tipine göre veriyi oku
            switch (keyType)
            {
                case RedisType.String:
                    var stringValue = await database.StringGetAsync(operation.Key);
                    operation.Value = stringValue.HasValue ? stringValue.ToString() : string.Empty;
                    operation.Success = stringValue.HasValue;
                    break;

                case RedisType.Hash:
                    var hashFields = await database.HashGetAllAsync(operation.Key);
                    operation.HashEntries = hashFields;
                    operation.Success = hashFields.Length > 0;
                    break;

                case RedisType.List:
                    var listValues = await database.ListRangeAsync(operation.Key);
                    operation.ListValues = listValues;
                    operation.Success = listValues.Length > 0;
                    break;

                case RedisType.Set:
                    var setValues = await database.SetMembersAsync(operation.Key);
                    operation.SetValues = setValues;
                    operation.Success = setValues.Length > 0;
                    break;

                case RedisType.SortedSet:
                    var sortedSetValues = await database.SortedSetRangeByRankWithScoresAsync(operation.Key);
                    operation.SortedSetEntries = sortedSetValues;
                    operation.Success = sortedSetValues.Length > 0;
                    break;

                default:
                    operation.Success = false;
                    break;
            }

            operation.Success = true;
        }
        catch
        {
            operation.Success = false;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private static async Task WriteKeyDataAsync(IDatabase database, RedisKeyOperationModel operation)
    {
        await Semaphore.WaitAsync();
        try
        {
            if (!operation.Success)
            {
                return;
            }

            switch (operation.KeyType)
            {
                case RedisType.String:
                    await database.StringSetAsync(operation.Key, operation.Value, operation.Ttl);
                    break;

                case RedisType.Hash:
                    if (operation.HashEntries.Length > 0)
                    {
                        await database.HashSetAsync(operation.Key, operation.HashEntries);
                        if (operation.Ttl.HasValue)
                        {
                            await database.KeyExpireAsync(operation.Key, operation.Ttl);
                        }
                    }

                    break;

                case RedisType.List:
                    if (operation.ListValues.Length > 0)
                    {
                        await database.ListRightPushAsync(operation.Key, operation.ListValues);
                        if (operation.Ttl.HasValue)
                        {
                            await database.KeyExpireAsync(operation.Key, operation.Ttl);
                        }
                    }

                    break;

                case RedisType.Set:
                    if (operation.SetValues.Length > 0)
                    {
                        await database.SetAddAsync(operation.Key, operation.SetValues);
                        if (operation.Ttl.HasValue)
                        {
                            await database.KeyExpireAsync(operation.Key, operation.Ttl);
                        }
                    }

                    break;

                case RedisType.SortedSet:
                    if (operation.SortedSetEntries.Length > 0)
                    {
                        await database.SortedSetAddAsync(operation.Key, operation.SortedSetEntries);
                        if (operation.Ttl.HasValue)
                        {
                            await database.KeyExpireAsync(operation.Key, operation.Ttl);
                        }
                    }

                    break;

                case RedisType.None:
                    operation.Success = true;
                    break;

                default:
                    operation.Success = false;
                    return;
            }

            operation.Success = true;
        }
        catch
        {
            operation.Success = false;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private static async Task DeleteSourceKeysAsync(IDatabase sourceDatabase, IEnumerable<RedisKeyOperationModel> successfulKeys)
    {
        var deleteKeys = successfulKeys.Where(x => x.Success).Select(x => (RedisKey)x.Key).ToArray();
        if (deleteKeys.Length > 0)
        {
            await sourceDatabase.KeyDeleteAsync(deleteKeys);
        }
    }
}