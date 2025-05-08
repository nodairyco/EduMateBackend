using EduMateBackend.Data;
using EduMateBackend.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EduMateBackend.Services;

public class UserService(EduMateDatabaseContext context)
{
    private readonly EduMateDatabaseContext _dbContext = context;
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(User user, string password)
        => _hasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string hashedPassword, string password)
        => _hasher.VerifyHashedPassword(user, hashedPassword, password) 
           == PasswordVerificationResult.Success;

    public async Task<Tuple<Errors, User?>> AddUserAsync(UserDto user)
    {

        var usernameUser = await FindByUsernameAsync(user.username);
        if (usernameUser != null)
        {
            return new Tuple<Errors, User?>(Errors.DuplicateUsername, null);
        }

        if (await FindByEmailAsync(user.email) != null)
        {
            return new Tuple<Errors, User?>(Errors.DuplicateEmail, null);
        }
        
        var hashedPasswordUser = new User
        { 
            email = user.email, username = user.username, 
            password = user.password
        };

        hashedPasswordUser.password = HashPassword(hashedPasswordUser, hashedPasswordUser.password);
        await _dbContext.users.AddAsync(hashedPasswordUser); 
        await _dbContext.SaveChangesAsync();

        return new Tuple<Errors, User?>(Errors.None, hashedPasswordUser);
    }

    public async Task<User?> FindByEmailAsync(string email)
        => await _dbContext.users.FirstOrDefaultAsync(u => u.email == email);

    public async Task<User?> FindByUsernameAsync(string username)
        => await _dbContext.users.FirstOrDefaultAsync(u => u.username == username);
    
    public async Task<List<User>> FindAllAsync()
        => await _dbContext.users.ToListAsync();

    public async Task<bool> UpdateByEmailAsync(string newEmail, string newUsername, string email)
    {
        var prevUser = await _dbContext.users.FindAsync(email);
        if (prevUser == null)
        {
            return false;
        }

        prevUser.email = newEmail;
        prevUser.username = newUsername;

        await _dbContext.SaveChangesAsync();
        return true;
    }
}