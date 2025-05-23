using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduMateBackend.Helpers;
using EduMateBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using EduMateBackend.Models;

namespace EduMateBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(UserService userService, IConfiguration configuration):ControllerBase
{
    private readonly UserService _service = userService;

    [HttpPost("/registerUser")]
    public async Task<ActionResult<User>> RegisterUser(UserDto request)
    {
        var errorTuple = await _service.AddUserAsync(request);
        return errorTuple.Item1 switch
        {
            Errors.DuplicateUsername => BadRequest("Username is taken"),
            Errors.DuplicateEmail => BadRequest("Email is taken"),
            _ => Ok(errorTuple.Item2)
        };
    }

    [HttpPost("/login")]
    public async Task<ActionResult<string>> LoginUser(UserCred request)
    {
        var user = await _service.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest("User with this email does not exist");
        }

        if (!_service.VerifyPassword(user, user.Password, request.Password))
        {
            return BadRequest("Password does not match email");
        }

        return Ok(CreateToken(user));
    }

    private string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration.GetValue<string>("AuthSettings:Token")!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var tokenDescriptor = new JwtSecurityToken(
            issuer:configuration.GetValue<string>("AuthSettings:Issuer"),
            audience:configuration.GetValue<string>("AuthSettings:Audience"),
            claims:claims,
            expires:DateTime.Now.AddHours(1),
            signingCredentials:creds
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}