using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EduMateBackend.Models;

public class Post
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId PostId { get; set; } = ObjectId.GenerateNewId();

    [BsonRepresentation(BsonType.String)] public Guid PosterId { get; set; }

    [BsonRepresentation(BsonType.Array)]
    public ICollection<PostAttachment> Attachments { get; set; } = new List<PostAttachment>();

    [BsonRepresentation(BsonType.DateTime)]
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    [MaxLength(1000)] public string Content { get; set; } = string.Empty;

    public int Likes { get; set; } = 0;
    public PostParent PostParent { get; set; } = null!;
}

public class PostParent
{
    public enum PostParentType
    {
        Group,
        Post,
        User
    }

    public string ParentId { get; set; } = string.Empty;
    [BsonRepresentation(BsonType.String)]
    public PostParentType ParentType { get; set; }
}

public class PostAttachment
{
    public string DownloadLink { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
}