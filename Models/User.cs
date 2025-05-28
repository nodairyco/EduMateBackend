using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EduMateBackend.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MinLength(3)] [MaxLength(50)] public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string AvatarId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.DateTime)]
    public DateTime SignUpDate { get; set; } = DateTime.UtcNow;

    public bool IsVerified { get; set; } = false;

    public ICollection<string> Following { get; set; } = new List<string>();
    public ICollection<string> Followers { get; set; } = new List<string>();
}