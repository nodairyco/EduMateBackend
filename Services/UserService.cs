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

        if (!followeeUser.Followers.Contains(follower) && !followerUser.Following.Contains(followee))
        {
            return Errors.UserNotFollowed;
        }

        followerUser.Following.Remove(followee);
        followeeUser.Followers.Remove(follower);
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

    public async Task<Tuple<Errors, string>> ChangeUserAvatarAsync(User user, IFormFile newAvatar)
    {
        var extension = Path.GetExtension(newAvatar.FileName);
        var newFileName = $"{user.Username}Avatar{extension}";
        var nonPathName = $"{user.Username}Avatar{extension}";

        var path = Path.Combine("Uploads", newFileName);

        await using (var stream = new FileStream(path, FileMode.Create))
        {
            await newAvatar.CopyToAsync(stream);
        }

        Uri downloadLink;
        
        try
        {
            _megaApiClient.Login(
                configuration.GetValue<string>("MegaSettings:Email")!,
                configuration.GetValue<string>("MegaSettings:Password")!
            );
            var nodes = await _megaApiClient.GetNodesAsync();
            var avatarFolder =
                nodes.Single(x => x.Name == "Avatars" && x.Type == NodeType.Directory);
            var uploadedAvatar =
                _megaApiClient.UploadFile
                (
                    $"{configuration.GetValue<string>("MegaSettings:Path")!}/{nonPathName}",
                    avatarFolder
                );
            downloadLink = await _megaApiClient.GetDownloadLinkAsync(uploadedAvatar);

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return new Tuple<Errors, string>(Errors.UnknownError, string.Empty);
        }

        user.AvatarUrl = downloadLink.ToString();
        await _dbContext.SaveChangesAsync();

        return new Tuple<Errors, string>(Errors.None, downloadLink.ToString());
    }

    private async Task SaveChange(User user)
    {
        await _userCollection.ReplaceOneAsync(u => u.Id == user.Id, user);
    }
}