namespace RedisKeyMover.Data.Entities;

public class RedisOperation
{
    public int Id { get; set; }
    public string SourceHost { get; set; } = null!;
    public int SourcePort { get; set; }
    public string SourcePassword { get; set; } = null!;
    public int SourceDatabase { get; set; }
    public string TargetHost { get; set; } = null!;
    public int TargetPort { get; set; }
    public string TargetPassword { get; set; } = null!;
    public int TargetDatabase { get; set; }
    public string Pattern { get; set; } = "*";
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Navigation Property
    public List<RedisOperationKey> Keys { get; set; } = [];
}