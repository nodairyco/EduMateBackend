using EduMateBackend.Data;
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
    
    public async Task<User?> AddUserAsync(User user)
    {
        try
        {
            var hashedPasswordUser =
                new User
                {
                    id = user.id, email = user.email, username = user.username,
                    password = HashPassword(user, user.password)
                };
            await _dbContext.users.AddAsync(hashedPasswordUser);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            return null;
        }

        return user;
    }

    public async Task<User?> FindByEmailAsync(string email)
        => await _dbContext.users.FindAsync(email);

    public async Task<List<User>> FindAllAsync()
        => await _dbContext.users.ToListAsync();
}