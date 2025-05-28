using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using EduMateBackend.Data;
using EduMateBackend.Helpers;
using EduMateBackend.Models;
using MongoDB.Driver;

namespace EduMateBackend.Services;

public class PostService(
    MongoDbDatabaseContext dbContext,
    CloudinaryService cloudinaryService,
    IConfiguration configuration)
{
    private readonly IMongoCollection<Post> _postCollection = dbContext.Posts;
    private readonly Cloudinary _cloudinary = cloudinaryService.Cloudinary;

    public async Task<Tuple<Errors, Post?>> UploadPostAsync(string content, Guid uploader,
        IFormFileCollection attachments, PostParent parent)
    {
        var attachmentList = new List<PostAttachment>();
        var post = new Post();
        foreach (var file in attachments)
        {
            try
            {
                attachmentList.Add(await UploadFile(file, post));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new Tuple<Errors, Post?>(Errors.UnknownError, null);
            } 
        }

        post.Attachments = attachmentList;
        post.PosterId = uploader;
        post.PostParent = parent;
        post.Content = content;
        await _postCollection.InsertOneAsync(post);
        return new Tuple<Errors, Post?>(Errors.None, post);
    }

    private async Task<PostAttachment> UploadFile(IFormFile file, Post post)
    {
        var extension = Path.GetExtension(file.FileName);
        var newFileName = $"{post.PostId}Attachment{file.FileName}{extension}";

        var path = Path.Combine("Uploads", newFileName);

        await using (var stream = new FileStream(path, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }


        var uploadParams = new ImageUploadParams()
        {
            File = new FileDescription
            {
                FileName = newFileName,
                FilePath = $"{configuration.GetValue<string>("CloudinarySettings:Path")}/{newFileName}"
            },
            UseFilename = true,
            UniqueFilename = false,
            Overwrite = true
        };
        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        var publicId = uploadResult.PublicId!;
        var url = uploadResult.Url!;
        return new PostAttachment { DownloadLink = url.ToString(), PublicId = publicId };
    }

    private async Task SaveChange(Post post)
        => await _postCollection.ReplaceOneAsync(p => p.PostId == post.PostId, post);
}