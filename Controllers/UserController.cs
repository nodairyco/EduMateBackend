using System.Net.Mail;
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
public class UserController(UserService userService, EmailService emailService) : Controller
{ 
    private readonly UserService _service = userService;
    private readonly EmailService _emailService = emailService;
    
    [HttpGet("/getSelf")]
    [Authorize]
    public async Task<ActionResult<User>> GetSelfAsync()
    {
        return Ok(await GetUserFromJwtAsync(HttpContext));
    }
    

    [HttpGet("/addFollower")]
    [Authorize(Policy = "VerifiedOnly")]
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
    [Authorize(Policy = "VerifiedOnly")]
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
    [Authorize(Policy = "VerifiedOnly")]
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
    [Authorize(Policy = "VerifiedOnly")]
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

    [HttpGet("/getChangePasskey")]
    public async Task<ActionResult> GetPasswordChangeKeyAsync(string email)
    {
        var errorTuple = await _service.GeneratePassKeyByEmailAsync(email);
        if (errorTuple.Item1 == Errors.UserNotFound)
            return BadRequest("No user with this email");
        var pct = errorTuple.Item2!;
        try
        {
            await _emailService.SendPasswordChangePassKeyAsync(email, pct.PassKey);
        }
        catch (SmtpFailedRecipientException e)
        {
            Console.WriteLine(e);
            return BadRequest("Invalid email");
        }

        return Ok("Reset key sent via email");
    }

    [HttpGet("/changePassword")]
    public async Task<ActionResult> ChangePasswordAsync(string email, string passkey, string newPassword)
    {
        var user = await _service.FindByEmailAsync(email);
        if (user == null)
            return BadRequest("User with this email doesn't exist");

        var passKeyVerification = await _service.VerifyPasskey(email, passkey);
        switch (passKeyVerification)
        {
            case Errors.PctNotFound or Errors.IncorrectPasskey:
                return BadRequest("Cannot change password as passkey doesn't match email");
            case Errors.PasskeyTooOld:
                return BadRequest("The given passkey is out of date. Generate new one.");
        }

        user = await _service.ChangePasswordAsync(user, newPassword);
        return Ok(user);
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
