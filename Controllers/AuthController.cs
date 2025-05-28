using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
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
public class AuthController(UserService userService, IConfiguration configuration, EmailService emailService)
    : ControllerBase
{
    private readonly UserService _service = userService;
    private readonly EmailService _emailService = emailService;

    [HttpPost("/registerUser")]
    public async Task<ActionResult<User>> RegisterUser(UserDto request)
    {
        var errorTuple = await _service.AddUserAsync(request);
        if (errorTuple.Item1 == Errors.DuplicateEmail)
            return BadRequest("Email is taken");

        if (errorTuple.Item1 == Errors.DuplicateUsername)
            return BadRequest("Username is taken");

        var user = errorTuple.Item2!;
        
        try
        {
            await _emailService.SendVerificationEmailAsync(user, CreateMinimalToken(user));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await _service.DeleteById(user.Id.ToString());
            return BadRequest("Invalid email user not added");
        }

        return user;
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

    [HttpGet("/verifyUserEmail")]
    public async Task<ActionResult> VerifyUserEmail([FromQuery] string? token)
    {
        if (token == null)
            return Ok("EmptyToken :(((");

        var handler = new JwtSecurityTokenHandler();
        string userId;
        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["AuthSettings:EmailToken"]!)
                )
            }, out var _);
            userId = principal.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)!.Value;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Ok("Invalid Token");
        }

        var user = await _service.FindByIdAsync(new Guid(userId));
        if (user == null)
            return Ok("No Such user :((");

        await _service.MakeVerified(user);

        return Ok("Yeah it's been done!");
    }

    private string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("Verification", user.IsVerified.ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration.GetValue<string>("AuthSettings:Token")!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: configuration.GetValue<string>("AuthSettings:Issuer"),
            audience: configuration.GetValue<string>("AuthSettings:Audience"),
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    private string CreateMinimalToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration.GetValue<string>("AuthSettings:EmailToken")!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}