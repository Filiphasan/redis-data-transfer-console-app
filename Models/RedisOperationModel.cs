using RedisKeyMover.Data.Entities;
using StackExchange.Redis;

namespace RedisKeyMover.Models;

public class RedisOperationModel
{
    public RedisOperation Operation { get; init; } = null!;
    public IDatabase SourceDatabase { get; set; } = null!;
    public IDatabase TargetDatabase { get; set; } = null!;
    public List<RedisKeyOperationModel> Keys { get; set; } = [];
    public int MaxParallelizm { get; set; }
    public int BatchCount { get; set; }
    public bool DeleteSourceKeys { get; set; }
    public bool IsFullOperation { get; set; }
    public bool ReadSourceNewKeys { get; set; }
    public bool RetryFailedKeysTransfer { get; set; }
}