using CG.Web.MegaApiClient;
using EduMateBackend.Data;
using EduMateBackend.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace EduMateBackend.Services;

public class UserService(EduMateDatabaseContext context, IConfiguration configuration)
{
    private readonly EduMateDatabaseContext _dbContext = context;
    private readonly PasswordHasher<User> _hasher = new();
    private readonly MegaApiClient _megaApiClient = new MegaApiClient();

    public string HashPassword(User user, string password)
        => _hasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string hashedPassword, string password)
        => _hasher.VerifyHashedPassword(user, hashedPassword, password) 
           == PasswordVerificationResult.Success;

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
            Password = user.Password
        };

        hashedPasswordUser.Password = HashPassword(hashedPasswordUser, hashedPasswordUser.Password);
        await _dbContext.Users.AddAsync(hashedPasswordUser); 
        await _dbContext.SaveChangesAsync();

        return new Tuple<Errors, User?>(Errors.None, hashedPasswordUser);
    }

    public async Task<User?> FindByEmailAsync(string email)
        => await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> FindByUsernameAsync(string username)
        => await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User?> FindByIdAsync(Guid id)
        => await _dbContext.Users.FindAsync(id);
    
    public async Task<List<User>> FindAllAsync()
        => await _dbContext.Users.ToListAsync();

    public async Task<Errors> AddFollowerByUsernameAsync(string follower, string followee)
    {
        var followerUser = await FindByUsernameAsync(follower);
        var followeeUser = await FindByUsernameAsync(followee);

        if (followeeUser == null || followerUser == null)
        {
            return Errors.UserNotFound;
        }

        var followTable = _dbContext.FollowerRelationships;
        if (await followTable.FirstOrDefaultAsync(
                f => f.FolloweeId == followeeUser.Id && f.FollowerId == followerUser.Id) != null)
        {
            return Errors.UserAlreadyFollowed;
        }

        followTable.Add(new Followers { FollowerId = followerUser.Id, FolloweeId = followeeUser.Id });
        await _dbContext.SaveChangesAsync();
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

        var followRelationship = await _dbContext.FollowerRelationships.FirstOrDefaultAsync(f =>
            f.FolloweeId == followeeUser.Id && f.FollowerId == followerUser.Id);
        
        if (followRelationship == null)
        {
            return Errors.UserNotFollowed;
        }

        _dbContext.FollowerRelationships.Remove(followRelationship);
        await _dbContext.SaveChangesAsync();
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

        await _dbContext.SaveChangesAsync() ;
        
        return Errors.None;
    }

    public async Task<Tuple<User?, Errors>> DeleteById(string id)
    {
        var guid = new Guid(id);
        var user = await FindByIdAsync(guid);
        if (user == null)
            return new Tuple<User?, Errors>(null, Errors.UserNotFound);

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
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
}