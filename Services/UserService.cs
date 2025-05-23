using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using EduMateBackend.Data;
using EduMateBackend.Helpers;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using User = EduMateBackend.Models.User;

namespace EduMateBackend.Services;

public class UserService(
    IConfiguration configuration,
    CloudinaryService cloudinaryService,
    MongoDbDatabaseContext mongoDbDatabaseContext
)
{
    private readonly IMongoCollection<User> _userCollection = mongoDbDatabaseContext.Users;
    private readonly PasswordHasher<User> _hasher = new();
    private readonly Cloudinary _cloudinary = cloudinaryService.Cloudinary;


    public string HashPassword(User user, string password)
        => _hasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string hashedPassword, string password)
        => _hasher.VerifyHashedPassword(user, hashedPassword, password)
           == PasswordVerificationResult.Success;

    public async Task<User?> FindByEmailAsync(string email)
        => await _userCollection.Find(u => u.Email == email)
            .FirstOrDefaultAsync();

    public async Task<User?> FindByUsernameAsync(string username)
        => await _userCollection.Find(u => u.Username == username)
            .FirstOrDefaultAsync();

    public async Task<User?> FindByIdAsync(Guid id)
        => await _userCollection.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task<Tuple<Errors, User?>> AddUserAsync(UserDto user)
    {
        var usernameUser = await FindByUsernameAsync(user.Username);
        if (usernameUser != null)
        {
            return new Tuple<Errors, User?>(Errors.DuplicateUsername, null);
        }

        if (await FindByEmailAsync(user.Email) != null)
        {
            return new Tuple<Errors, User?>(Errors.DuplicateEmail, null);
        }

        var hashedPasswordUser = new User
        {
            Email = user.Email, Username = user.Username,
            Password = user.Password,
            Bio = user.Bio
        };

        hashedPasswordUser.Password = HashPassword(hashedPasswordUser, hashedPasswordUser.Password);
        await _userCollection.InsertOneAsync(hashedPasswordUser);

        return new Tuple<Errors, User?>(Errors.None, hashedPasswordUser);
    }

    public async Task<Errors> ChangeBioByUsernameAsync(string username, string bio)
    {
        var user = await FindByUsernameAsync(username);
        if (user == null)
            return Errors.UserNotFound;
        user.Bio = bio;
        await SaveChange(user);
        return Errors.None;
    }


    public async Task<Errors> AddFollowerByUsernameAsync(string follower, string followee)
    {
        var followerUser = await FindByUsernameAsync(follower);
        var followeeUser = await FindByUsernameAsync(followee);

        if (followeeUser == null || followerUser == null)
        {
            return Errors.UserNotFound;
        }

        if (followeeUser.Followers.Contains(followerUser.Id.ToString()) &&
            followerUser.Following.Contains(followeeUser.Id.ToString()))
        {
            return Errors.UserAlreadyFollowed;
        }

        followerUser.Following.Add(followeeUser.Id.ToString());
        followeeUser.Followers.Add(followerUser.Id.ToString());
        await SaveChange(followeeUser);
        await SaveChange(followerUser);

        return Errors.None;
    }

    public async Task<Errors> RemoveFollowerByUsernameAsync(string follower, string followee)
    {
        var followerUser = await FindByUsernameAsync(follower);
        var followeeUser = await FindByUsernameAsync(followee);

        if (followeeUser == null || followerUser == null)
        {
            return Errors.UserNotFound;
        }

        if (!followeeUser.Followers.Contains(followerUser.Id.ToString()) &&
            !followerUser.Following.Contains(followeeUser.Id.ToString()))
        {
            return Errors.UserNotFollowed;
        }

        followerUser.Following.Remove(followeeUser.Id.ToString());
        followeeUser.Followers.Remove(followerUser.Id.ToString());
        await SaveChange(followeeUser);
        await SaveChange(followerUser);

        return Errors.None;
    }

    public async Task<Errors> UpdateByEmailAsync(UserDto newData, string email)
    {
        var user = await FindByEmailAsync(email);
        if (user == null)
        {
            return Errors.UserNotFound;
        }

        if (newData.Email != user.Email && await FindByEmailAsync(newData.Email) != null)
        {
            return Errors.DuplicateEmail;
        }

        if (newData.Username != user.Username && await FindByUsernameAsync(newData.Username) != null)
        {
            return Errors.DuplicateUsername;
        }

        user.Username = newData.Username;
        user.Email = newData.Email;

        await SaveChange(user);

        return Errors.None;
    }

    public async Task<Tuple<User?, Errors>> DeleteById(string id)
    {
        var guid = new Guid(id);
        var user = await FindByIdAsync(guid);
        if (user == null)
            return new Tuple<User?, Errors>(null, Errors.UserNotFound);

        await _userCollection.DeleteOneAsync(u => u.Id == guid);
        return new Tuple<User?, Errors>(user, Errors.None);
    }

    public async Task<Tuple<Errors, string?>> ChangeUserAvatarAsync(User user, IFormFile newAvatar)
    {
        var extension = Path.GetExtension(newAvatar.FileName);
        var newFileName = $"{user.Username}Avatar{extension}";

        var path = Path.Combine("Uploads", newFileName);

        await using (var stream = new FileStream(path, FileMode.Create))
        {
            await newAvatar.CopyToAsync(stream);
        }

        string publicId;
        Uri url;

        try
        {
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
            publicId = uploadResult.PublicId!;
            url = uploadResult.Url!;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new Tuple<Errors, string?>(Errors.UnknownError, null);
        }

        user.AvatarUrl = url.ToString();
        user.AvatarId = publicId;

        await SaveChange(user);
        var uploadsDirectory = new DirectoryInfo(
            $"{configuration.GetValue<string>("CloudinarySettings:Path")}");
        foreach (var file in uploadsDirectory.GetFiles())
        {
            file.Delete();
        }

        return new Tuple<Errors, string?>(Errors.None, url.ToString());
    }

    private async Task SaveChange(User user)
    {
        await _userCollection.ReplaceOneAsync(u => u.Id == user.Id, user);
    }
}