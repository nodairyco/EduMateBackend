using EduMateBackend.Data;
using EduMateBackend.Services;
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
    public async Task<ActionResult<User>> FindUserByEmail(string email)
    {
        var user = await _service.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }
}
