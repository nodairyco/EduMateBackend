using System.Security.Claims;
using EduMateBackend.Data;
using EduMateBackend.Helpers;
using EduMateBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMateBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(UserService userService) : Controller
{ 
    private readonly UserService _service = userService;

    [HttpGet("/getAll")]
    public async Task<ActionResult<List<User>>> FindAllUsersAsync()
    {
        var users = await _service.FindAllAsync();
        return Ok(users);
    }

    [HttpGet("/getByEmail")]
    public async Task<ActionResult<User>> FindUserByEmailAsync(string email)
    {
        var user = await _service.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPatch("/updateUserData")]
    [Authorize]
    public async Task<ActionResult<User>> UpdateUserDataAsync
    (UserDto userDto)
    {
        var user = await GetUserFromJwtAsync(HttpContext);
        var error = await _service.UpdateByEmailAsync(userDto, user.Email);
        
        return error switch
        {
            Errors.DuplicateUsername => BadRequest("Cannot have duplicate Username"),
            Errors.DuplicateEmail => BadRequest("Cannot have duplicate Email"),
            Errors.UserNotFound => NotFound("User with this email not found"),
            _ => Ok(await _service.FindByEmailAsync(userDto.Email)) 
        };
    }

    [HttpGet("/addFollower")]
    [Authorize]
    public async Task<ActionResult<User>> AddFollowerByUsernameAsync(string followeeUsername)
    {
        var user = await GetUserFromJwtAsync(HttpContext);
        var error = await _service.AddFollowerByUsernameAsync(user.Username, followeeUsername);
        return error switch
        {
            Errors.UserNotFound => BadRequest("User with this username doesn't exist"),
            Errors.UserAlreadyFollowed => BadRequest("User already followed"),
            _ => Ok(user)
        };
    }

    [HttpGet("/removeFollower")]
    [Authorize]
    public async Task<ActionResult<string>> RemoveFollowerByUsernameAsync(string followeeUsername)
    {
        var user = await GetUserFromJwtAsync(HttpContext);
        var errors = await _service.RemoveFollowerByUsernameAsync(user.Username, followeeUsername);
        return errors switch
        {
            Errors.UserNotFound => BadRequest("User with this Username doesn't exist"),
            Errors.UserNotFollowed => BadRequest("User with this Username not followed"),
            _ => Ok("User successfully removed from following list")  
        };
    }

    [HttpDelete("/deleteSelf")]
    [Authorize]
    public async Task<ActionResult<User>> DeleteSelfAsync()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guid = identity!.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var errors = await _service.DeleteById(guid);
        return Ok(errors.Item1!);
    }
    
    private async Task<User> GetUserFromJwtAsync(HttpContext httpContext)
    {
        var identity = httpContext.User.Identity as ClaimsIdentity;
        var guid = string.Empty;
        if (identity != null)
        {
            var claims = identity.Claims;
            guid = identity.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        }
        var user = await _service.FindByIdAsync(new Guid(guid));
        return user!;
    }
}
