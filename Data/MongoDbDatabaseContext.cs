using EduMateBackend.Models;
using MongoDB.Driver;

namespace EduMateBackend.Data;

public class MongoDbDatabaseContext
{
    private readonly IMongoDatabase _database;

    public MongoDbDatabaseContext(IConfiguration configuration)
    {
        var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
        _database = client.GetDatabase("EduMate");
    }

    public IMongoCollection<User> Users => _Database.GetCollection<EduMateBackend.Models.User>("Users");
    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");

    public IMongoCollection<PasswordChangeTable> PCT =>
        _database.GetCollection<PasswordChangeTable>("PasswordChangeTable");
}