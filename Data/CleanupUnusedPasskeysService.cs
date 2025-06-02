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
            var cutoffTime = DateTime.UtcNow.AddMinutes(-10);

            var filter = Builders<PasswordChangeTable>.Filter.Lt(pct => pct.CreationDate, cutoffTime);
            var result = await _pctCollection.DeleteManyAsync(filter, stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}