namespace RedisKeyMover.Data.Entities;

public class RedisOperationKey
{
    public int Id { get; set; }
    public int OperationId { get; set; }
    public string Key { get; set; } = null!;
    public int KeyType { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Navigation Property
    public RedisOperation Operation { get; set; } = null!;
}