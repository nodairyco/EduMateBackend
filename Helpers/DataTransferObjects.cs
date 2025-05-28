using System.ComponentModel.DataAnnotations;

namespace EduMateBackend.Helpers;

public class UserDto
{
    [MinLength(3)]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    [MinLength(8)]
    [MaxLength(255)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(100)] public string Bio { get; set; } = string.Empty;
}

public class UserCred
{
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    [MinLength(8)]
    [MaxLength(255)]
    public string Password { get; set; } = string.Empty;
}