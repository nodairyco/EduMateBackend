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

    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    public IMongoCollection<Post> Posts => _database.GetCollection<Post>("Posts");

    public IMongoCollection<PasswordChangeTable> Pct =>
        _database.GetCollection<PasswordChangeTable>("PasswordChangeTable");
}