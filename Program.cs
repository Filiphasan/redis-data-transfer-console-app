using System.Diagnostics;
using RedisKeyMover.Helpers;
using RedisKeyMover.Models;
using RedisKeyMover.Redis;

ConsoleHelper.WriteLineWithSeperator("Redis Veri Taşıma Uygulaması");

RedisOperationModel? operationModel;
try
{
    operationModel = await RedisTransferDataHelper.GetOperationAsync();
}
catch (Exception ex)
{
    ConsoleHelper.WriteError($"Hata: {ex.Message}");
    return;
}

RedisConfigRecord sourceConfig;
RedisConfigRecord targetConfig;
try
{
    sourceConfig = RedisConnectionHelper.ReadConfig(operationModel, true);
    targetConfig = RedisConnectionHelper.ReadConfig(operationModel, false);
}
catch (Exception ex)
{
    ConsoleHelper.WriteError($"Hata: {ex.Message}");
    return;
}

ConsoleHelper.WriteInfo("Girilen Bilgiler:");
ConsoleHelper.WriteInfo($"-> Kaynak Redis: {sourceConfig.ConnectionString} (DB: {sourceConfig.Database})");
ConsoleHelper.WriteInfo($"-> Hedef Redis: {targetConfig.ConnectionString} (DB: {targetConfig.Database})");

ConsoleHelper.WriteLineWithSeperator("");
if (!ConsoleHelper.ReadLineAsAccept("Devam etmek istiyor musunuz?"))
{
    return;
}

operationModel ??= await RedisTransferDataHelper.CreateOperationAsync(sourceConfig, targetConfig);

// Redis bağlantılarını test et
ConsoleHelper.WriteLineWithSeperator("Redis bağlantıları test ediliyor...", false);

await using var sourceRedis = new RedisService(sourceConfig.ConnectionString);
await using var targetRedis = new RedisService(targetConfig.ConnectionString);

var isSourcesReady = await RedisConnectionHelper.TestConnectionsAsync(sourceRedis, targetRedis);
if (!isSourcesReady)
{
    ConsoleHelper.WriteError("Hata: Redis bağlantıları başarısız! İşlem iptal edildi.");
    return;
}

ConsoleHelper.WriteLineWithSeperator("");
var maxParallelCountStr = ConsoleHelper.ReadLine("Max Paralel İşlem Sayısını Girin (1-100):", false);
if (string.IsNullOrWhiteSpace(maxParallelCountStr))
{
    maxParallelCountStr = "10";
}

if (!int.TryParse(maxParallelCountStr, out var maxParallelCount) || maxParallelCount < 1 || maxParallelCount > 100)
{
    ConsoleHelper.WriteError("Hata: Geçersiz değer girdiniz!");
    return;
}

var batchCountInput = ConsoleHelper.ReadLine("Batch İşlem Sayısını Girin (100-10000):", false);
if (string.IsNullOrWhiteSpace(batchCountInput))
{
    batchCountInput = "1000";
}

if (!int.TryParse(batchCountInput, out var batchCount) || batchCount < 100 || batchCount > 10000)
{
    ConsoleHelper.WriteError("Hata: Geçersiz değer girdiniz!");
    return;
}

operationModel.MaxParallelizm = maxParallelCount;
operationModel.BatchCount = batchCount;
RedisDataHelper.Semaphore = new SemaphoreSlim(maxParallelCount, maxParallelCount);

ConsoleHelper.WriteLineWithSeperator("");
operationModel.DeleteSourceKeys = ConsoleHelper.ReadLineAsAccept("Başarılı taşınan verilerin kaynaktan silinmesini istiyor musunuz?");
if (operationModel.DeleteSourceKeys)
{
    ConsoleHelper.WriteInfo("Başarılı taşınan verilerin kaynaktan silinecektir, Tercih sizin ;)");
}

ConsoleHelper.WriteLineWithSeperator("");
var pattern = ConsoleHelper.ReadLine("Anahtar pattern'i girin (* tüm anahtarlar için, örn: user:* veya cache:*):", false);
if (string.IsNullOrWhiteSpace(pattern))
{
    pattern = "*";
}

operationModel.Operation.Pattern = pattern;

var sourceDatabase = sourceRedis.GetDatabase(sourceConfig.Database);
var targetDatabase = targetRedis.GetDatabase(targetConfig.Database);
operationModel.SourceDatabase = sourceDatabase;
operationModel.TargetDatabase = targetDatabase;


ConsoleHelper.WriteInfo($"Pattern: {pattern}");
ConsoleHelper.Write("Anahtar listesi alınıyor... Lütfen bekleyiniz. Bu işlem biraz zaman alabilir.");

var watch = Stopwatch.StartNew();
await operationModel.FindAndSaveKeysAsync();

watch.Stop();
ConsoleHelper.WriteInfo($"Kaynak Redis'de {watch.ElapsedMilliseconds:N0} ms sürede {operationModel.Keys.Count:N0} adet işlenebilecek anahtar bulundu.");
ConsoleHelper.WriteLineWithSeperator("", false);
if (!ConsoleHelper.ReadLineAsAccept("Devam etmek istiyor musunuz?"))
{
    return;
}

ConsoleHelper.Write("Anahtarlar taşınıyor...");

await operationModel.StartTransferOperationAsync();

ConsoleHelper.WriteLineWithSeperator("", false);
ConsoleHelper.WriteSuccess("Anahtarlar başarıyla taşındı, Kapatmak için ENTER tuşuna basınız.");
ConsoleHelper.ReadLine("");