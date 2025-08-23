using StackExchange.Redis;

namespace RedisKeyMover.Models;

public class RedisKeyOperationModel
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Success { get; set; }
    public RedisType KeyType { get; set; }
    public TimeSpan? Ttl { get; set; }
    public RedisValue[] ListValues { get; set; } = [];
    public HashEntry[] HashEntries { get; set; } = [];
    public RedisValue[] SetValues { get; set; } = [];
    public SortedSetEntry[] SortedSetEntries { get; set; } = [];
}