using System.Security.Claims;
using chatpro.Data;
using chatpro.DTOs;
using chatpro.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace chatpro.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;

        public AuthController(AppDbContext context, JwtService jwt)
        {
            _context = context;
            _jwt = jwt;
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
            if (userIdClaim == null)
                return Unauthorized("Invalid token.");

            var userId = Guid.Parse(userIdClaim.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return Unauthorized("User not found.");

            // Save file to wwwroot/uploads
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update user's profile picture URL
            user.ProfilePictureUrl = $"/uploads/{fileName}";
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile picture updated successfully", url = user.ProfilePictureUrl });
        }

        [HttpGet("check")]
        [Authorize]
        public async Task<IActionResult> Check()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token." });

            var userId = Guid.Parse(userIdClaim.Value);
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
                Token = null! // Optional
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // In JWT, logout usually means the client deletes the token.
            // You could implement a blacklist if needed.
            return Ok(new { message = "Logged out successfully (client-side token deletion)." });
        }

        [HttpPost("refresh")]
        [Authorize]
        public async Task<IActionResult> Refresh()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Invalid token.");

            var userId = Guid.Parse(userIdClaim.Value);
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
