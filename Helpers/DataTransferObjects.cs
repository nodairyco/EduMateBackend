using System.ComponentModel.DataAnnotations;

namespace EduMateBackend.Helpers;

public class UserDto
{
    [MinLength(8)]
    [MaxLength(255)]
    public string username { get; set; } = string.Empty;
    [MaxLength(255)]
    public string email { get; set; } = string.Empty;
    [MinLength(8)]
    [MaxLength(255)]
    public string password { get; set; } = string.Empty;
}

public class UserCred
{
    [MaxLength(255)]
    public string email { get; set; } = string.Empty;
    [MinLength(8)]
    [MaxLength(255)]
    public string password { get; set; } = string.Empty;
}