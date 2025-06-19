using System.Security.Claims;
using chatpro.Data;
using chatpro.DTOs;
using chatpro.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace chatpro.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;
        private readonly IHubContext<ChatHub> _hub;

        public AuthController(AppDbContext context, JwtService jwt, IHubContext<ChatHub> hub)
        {
            _context = context;
            _jwt = jwt;
            _hub = hub;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (_context.Users.Any(u => u.Email == request.Email))
                return BadRequest("Email already in use.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName,
                ProfilePictureUrl = request.ProfilePictureUrl,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _jwt.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Id = user.Id,
                Token = token,
                Email = user.Email,
                FullName = user.FullName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CreatedAt = user.CreatedAt
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(AuthRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            var token = _jwt.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Id = user.Id,
                Token = token,
                Email = user.Email,
                FullName = user.FullName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CreatedAt = user.CreatedAt,
            });
        }

        [HttpPost("update-profilepic")]
        [Authorize]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("Invalid token.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized("User not found.");

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);

            try
            {
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"File upload failed: {ex.Message}");
            }

            user.ProfilePictureUrl = $"/uploads/{fileName}";
            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();

                // ✅ Broadcast profile picture update via SignalR
                await _hub.Clients.All.SendAsync("UserProfileUpdated", new
                {
                    userId = user.Id,
                    profilePictureUrl = user.ProfilePictureUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to update DB: {ex.Message}");
            }

            return Ok(new AuthResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CreatedAt = user.CreatedAt,
                Token = null!
            });
        }

        [HttpGet("check")]
        [Authorize]
        public async Task<IActionResult> Check()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "Invalid token." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized(new { message = "User not found." });

            return Ok(new AuthResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CreatedAt = user.CreatedAt,
                Token = null!
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // JWT logout = delete token on client-side
            return Ok(new { message = "Logged out successfully." });
        }

        [HttpPost("refresh")]
        [Authorize]
        public async Task<IActionResult> Refresh()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("Invalid token.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized("User not found.");

            var newToken = _jwt.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Id = user.Id,
                Token = newToken,
                Email = user.Email,
                FullName = user.FullName,
                ProfilePictureUrl = user.ProfilePictureUrl
            });
        }
    }
}
