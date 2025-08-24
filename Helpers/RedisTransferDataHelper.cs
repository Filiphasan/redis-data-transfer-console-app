using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using RedisKeyMover.Data.Contexts;
using RedisKeyMover.Data.Entities;
using RedisKeyMover.Models;

namespace RedisKeyMover.Helpers;

public static class RedisTransferDataHelper
{
    public static async Task<RedisOperationModel?> GetOperationAsync()
    {
        await using var dbContext = AppDbContextFactory.Instance.CreateContext();
        var operationExist = await dbContext.RedisOperations.AnyAsync();
        if (!operationExist)
        {
            return null;
        }

        if (!ConsoleHelper.ReadLineAsAccept("Son 10 transfer işlemini görmek ister misiniz? (Yarıda kalan işlemlerinize devam için tavsiye edilir)"))
        {
            return null;
        }

        var redisOperations = await dbContext.RedisOperations.OrderByDescending(x => x.Id).Take(10).ToListAsync();
        foreach (var redisOperation in redisOperations)
        {
            Console.WriteLine(
                $"ID: {redisOperation.Id:4} - {redisOperation.SourceHost} (DB:{redisOperation.SourceDatabase:2}) -> {redisOperation.TargetHost} (DB:{redisOperation.TargetDatabase:2}) Tarihi: {redisOperation.StartTime:dd.MM.yyyy HH:mm}"
            );
        }

        var idInput = ConsoleHelper.ReadLine("Lütfen devam etmek istediğiniz transfer işleminin ID'sini giriniz:");
        if (!int.TryParse(idInput, out int id) || !redisOperations.Exists(x => x.Id == id))
        {
            throw new ArgumentException("Girilen değer hatalıdır veya listede yoktur!");
        }

        var operation = redisOperations.First(x => x.Id == id);
        var operationKeyCount = await dbContext.RedisOperationKeys.CountAsync(x => x.OperationId == id);
        var operationSuccessKeyCount = await dbContext.RedisOperationKeys.CountAsync(x => x.OperationId == id && x.Success);
        var operationFailKeyCount = await dbContext.RedisOperationKeys.CountAsync(x => x.OperationId == id && !x.Success);
        ConsoleHelper.WriteInfo($"Seçilen transfer işleminde {operationKeyCount} adet key vardır, Başarılı: {operationSuccessKeyCount}, Başarısız: {operationFailKeyCount}");

        var operationModel = new RedisOperationModel
        {
            Operation = operation,
            IsFullOperation = false,
            ReadSourceNewKeys = ConsoleHelper.ReadLineAsAccept("Seçilen transfer işleminde kaynaktan yeni oluşan keyler kontrol edilsin istiyor musunuz?"),
            RetryFailedKeysTransfer = false
        };

        if (ConsoleHelper.ReadLineAsAccept("Seçilen transfer işleminin en baştan tekrar etmesini ister misiniz?"))
        {
            operationModel.IsFullOperation = true;
        }

        if (!operationModel.IsFullOperation)
        {
            operationModel.RetryFailedKeysTransfer = ConsoleHelper.ReadLineAsAccept("Seçilen transfer işleminde hatalı olan keyleri yeniden denemek istiyor musunuz?");
        }

        if (operationModel is { IsFullOperation: false, ReadSourceNewKeys: false, RetryFailedKeysTransfer: false })
        {
            throw new InvalidOperationException("Teknik olarak seçimlerinizden anlaşılana göre yapılabilecek bir işlem yok, görüşürüz ;)");
        }

        return operationModel;
    }

    public static async Task<RedisOperationModel> CreateOperationAsync(RedisConfigRecord sourceConfig, RedisConfigRecord targetConfig)
    {
        await using var dbContext = AppDbContextFactory.Instance.CreateContext();
        var operation = new RedisOperation
        {
            SourceHost = sourceConfig.Host,
            SourcePort = sourceConfig.Port,
            SourceDatabase = sourceConfig.Database,
            SourcePassword = sourceConfig.Password,
            TargetHost = targetConfig.Host,
            TargetPort = targetConfig.Port,
            TargetDatabase = targetConfig.Database,
            TargetPassword = targetConfig.Password,
            Pattern = "*",
            StartTime = DateTime.Now,
            EndTime = DateTime.Now,
            SuccessCount = 0,
            FailCount = 0,
        };
        await dbContext.RedisOperations.AddAsync(operation);
        await dbContext.SaveChangesAsync();
        return new RedisOperationModel { Operation = operation, IsFullOperation = true };
    }

    public static async Task FindAndSaveKeysAsync(this RedisOperationModel operationModel)
    {
        await using var dbContext = AppDbContextFactory.Instance.CreateContext();
        if (operationModel.IsFullOperation)
        {
            await dbContext.RedisOperationKeys.Where(x => x.OperationId == operationModel.Operation.Id).ExecuteDeleteAsync();
            await FindKeyForNewOperationAsync(operationModel);
            return;
        }

        await FindKeyForExistingOperationAsync(operationModel);
    }

    private static async Task FindKeyForNewOperationAsync(RedisOperationModel operationModel)
    {
        var sourceAllKeys = await operationModel.SourceDatabase.ReadAllKeysAsync(operationModel.Operation.Pattern);
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 10 };
        await Parallel.ForEachAsync(sourceAllKeys, parallelOptions, async (key, ct) =>
        {
            await using var dbContext = AppDbContextFactory.Instance.CreateContext();
            var entity = new RedisOperationKey { Key = key.Key, OperationId = operationModel.Operation.Id, Success = false, KeyType = (int)key.KeyType };
            await dbContext.RedisOperationKeys.AddAsync(entity, ct);
            await dbContext.SaveChangesAsync(ct);
            key.Id = entity.Id;
        });

        operationModel.Keys = sourceAllKeys;
    }

    private static async Task FindKeyForExistingOperationAsync(RedisOperationModel operationModel)
    {
        await using var dbContext = AppDbContextFactory.Instance.CreateContext();
        List<RedisKeyOperationModel> keys = [];
        var allDbKeys = await dbContext.RedisOperationKeys.Where(x => x.OperationId == operationModel.Operation.Id)
            .Select(x => new { x.Id, x.Key, x.Success })
            .ToListAsync();
        if (operationModel.ReadSourceNewKeys)
        {
            var sourceAllKeys = await operationModel.SourceDatabase.ReadAllKeysAsync(operationModel.Operation.Pattern);
            var newKeys = sourceAllKeys.Where(x => !allDbKeys.Exists(y => y.Key == x.Key)).ToList();
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 10 };
            await Parallel.ForEachAsync(newKeys, parallelOptions, async (key, ct) =>
            {
                await using var dbContextInner = AppDbContextFactory.Instance.CreateContext();
                var entity = new RedisOperationKey { Key = key.Key, OperationId = operationModel.Operation.Id, Success = false, KeyType = (int)key.KeyType };
                await dbContextInner.RedisOperationKeys.AddAsync(entity, ct);
                await dbContextInner.SaveChangesAsync(ct);
                key.Id = entity.Id;
            });
            keys.AddRange(newKeys);
            newKeys.Clear();
            sourceAllKeys.Clear();
        }

        if (operationModel.RetryFailedKeysTransfer)
        {
            var failedKeys = allDbKeys.AsParallel().Where(x => !x.Success).Select(x => new RedisKeyOperationModel { Id = x.Id, Key = x.Key });
            keys.AddRange(failedKeys);
        }

        allDbKeys.Clear();
        operationModel.Keys = keys;
    }

    public static async Task StartTransferOperationAsync(this RedisOperationModel operationModel)
    {
        var watch = Stopwatch.StartNew();
        var chunkList = operationModel.Keys.Chunk(operationModel.BatchCount).ToList();
        operationModel.Keys.Clear();
        var chunkIndex = 0;
        var totalChunks = chunkList.Count;
        var totalSuccessCount = 0;
        var totalFailCount = 0;
        long totalElapsedMilliseconds = 0;

        foreach (var keys in chunkList)
        {
            watch.Restart();
            chunkIndex++;

            var list = keys.ToList();
            await operationModel.SourceDatabase.TransferKeysAsync(operationModel.TargetDatabase, list, operationModel.DeleteSourceKeys);
            var successCount = list.Count(r => r.Success);
            var failCount = list.Count - successCount;
            totalSuccessCount += successCount;
            totalFailCount += failCount;

            list.Clear();
            watch.Stop();
            var elapsed = watch.ElapsedMilliseconds;
            totalElapsedMilliseconds += elapsed;
            ConsoleHelper.WriteInfo($"\rPaket {chunkIndex:N0}/{totalChunks:N0} - Başarılı: {successCount:N0}, Başarısız: {failCount:N0}, Süre: {elapsed:N0} ms");
        }

        await using var dbContext = AppDbContextFactory.Instance.CreateContext();
        await dbContext.RedisOperations.Where(x => x.Id == operationModel.Operation.Id).ExecuteUpdateAsync(x => x.SetProperty(p => p.EndTime, DateTime.Now));
        ConsoleHelper.WriteInfo($"\rToplamda {totalSuccessCount:N0} adet anahtar başarıyla taşındı, {totalFailCount:N0} adet anahtar taşınamadı. Toplam Süre: {totalElapsedMilliseconds:N0} ms");

        chunkList.Clear();
    }
}