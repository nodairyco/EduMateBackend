using System.Security.Claims;
using EduMateBackend.Data;
using EduMateBackend.Helpers;
using EduMateBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using User = EduMateBackend.Models.User;

namespace EduMateBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(UserService userService) : Controller
{ 
    private readonly UserService _service = userService;
    
    [HttpGet("/getSelf")]
    [Authorize]
    public async Task<ActionResult<User>> GetSelfAsync()
    {
        return Ok(await GetUserFromJwtAsync(HttpContext));
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

    [HttpPost("/changeAvatar")]
    [Authorize]
    public async Task<ActionResult<string>> ChangeAvatarAsync([FromForm] IFormFile newAvatar)
    {
        if (newAvatar == null || newAvatar.Length == 0)
        {
            return BadRequest("Empty Image");
        }

        var user = await GetUserFromJwtAsync(HttpContext);

        var response = await _service.ChangeUserAvatarAsync(user, newAvatar);

        return response.Item1 switch
        {
            Errors.UnknownError => BadRequest("Unknown error occured"),
            _ => Ok(response.Item2)
        };
    }

    [HttpPost("/changeBio")]
    [Authorize]
    public async Task<ActionResult> ChangeBioAsync(string bio)
    {
        var user = await GetUserFromJwtAsync(HttpContext);
        var errors = await _service.ChangeBioByUsernameAsync(user.Username, bio);
        return errors switch
        {
            Errors.UserNotFound => NotFound(),
            _ => Ok()
        };
    }

    [HttpGet("/getUserBio/{username}")]
    public async Task<ActionResult> GetUserBioAsync(string username)
    {
        var user = await _service.FindByUsernameAsync(username);
        if (user == null)
        {
            return NotFound();
        }

        return Content(user.Bio, "text/html");
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
