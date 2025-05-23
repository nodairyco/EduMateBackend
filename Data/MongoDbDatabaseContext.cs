using EduMateBackend.Models;
using MongoDB.Driver;

namespace EduMateBackend.Data;

public class MongoDbDatabaseContext
{
    public readonly IMongoDatabase _Database;

    public MongoDbDatabaseContext(IConfiguration configuration)
    {
        var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
        _Database = client.GetDatabase("EduMate");
    }

    public IMongoCollection<User> Users => _Database.GetCollection<EduMateBackend.Models.User>("Users");
}