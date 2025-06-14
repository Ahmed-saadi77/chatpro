using System.Security.Claims;
using chatpro.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }

    // Get all users except current
    [HttpGet("all")]
    public async Task<IActionResult> GetAllUsersExceptCurrent()
    {
        var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var users = await _context.Users
            .Where(u => u.Id != currentUserId)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.ProfilePictureUrl
            })
            .ToListAsync();

        return Ok(users);
    }

    // Get current user info
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var user = await _context.Users
            .Where(u => u.Id == currentUserId)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.ProfilePictureUrl
            })
            .FirstOrDefaultAsync();

        return Ok(user);
    }
}
