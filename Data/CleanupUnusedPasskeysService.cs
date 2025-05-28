using EduMateBackend.Models;
using MongoDB.Driver;

namespace EduMateBackend.Data;

public class CleanupUnusedPasskeysService(MongoDbDatabaseContext dbContext) : BackgroundService
{
    private readonly IMongoCollection<PasswordChangeTable> _pctCollection = dbContext.Pct;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _pctCollection.DeleteManyAsync(pct => (DateTime.UtcNow - pct.CreationDate).TotalMinutes > 10,
                cancellationToken: stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}