using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EduMateBackend.Models;

public class PasswordChangeTable
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Email { get; set; } = string.Empty;
    public string PassKey { get; set; } = string.Empty;
    [BsonRepresentation(BsonType.DateTime)] 
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
}