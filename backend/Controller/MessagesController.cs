using System.Security.Claims;
using chatpro.Data;
using chatpro.DTOs;
using chatpro.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessagesController(AppDbContext context, IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // ✅ Get all messages for the logged-in user (sent and received)
    [HttpGet("all")]
    public async Task<IActionResult> GetAllMessagesForUser()
    {
        var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var messages = await _context.Messages
            .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.SenderId,
                m.ReceiverId,
                m.Text,
                m.ImageUrl,
                m.CreatedAt
            })
            .ToListAsync();

        return Ok(messages);
    }

    // ✅ Get all unique users the logged-in user has messaged with
    [HttpGet("users")]
    public async Task<IActionResult> GetMessageUsers()
    {
        var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var userIds = await _context.Messages
            .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
            .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
            .Distinct()
            .ToListAsync();

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
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


    // ✅ Send a message (text or image)
    [HttpPost]
    public async Task<IActionResult> SendMessage()
    {
        try
        {
            var senderId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var form = Request.Form;
            var receiverId = Guid.Parse(form["receiverId"]);
            var text = form["text"];
            var imageFile = form.Files["image"];

            string? imageUrl = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{imageFile.FileName}";
                var savePath = Path.Combine("wwwroot/uploads", fileName);
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                imageUrl = $"/uploads/{fileName}";
            }

            var message = new Message
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = receiverId,
                Text = text,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // ✅ SignalR real-time push to recipient
            if (ChatHub.userConnectionMap.TryGetValue(receiverId.ToString(), out var connectionId))
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", new
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Text = text,
                    ImageUrl = imageUrl,
                    message.CreatedAt
                });

                Console.WriteLine($"✅ Sent real-time message to {receiverId} via SignalR");
            }

            return Ok(message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }




    // ✅ Fetch all messages between current user and another user
    [HttpGet("{receiverId}")]
    public async Task<IActionResult> GetChatHistory(Guid receiverId)
    {
        var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var messages = await _context.Messages
            .Where(m =>
                (m.SenderId == currentUserId && m.ReceiverId == receiverId) ||
                (m.SenderId == receiverId && m.ReceiverId == currentUserId))
            .OrderBy(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.SenderId,
                m.ReceiverId,
                m.Text,
                m.ImageUrl,
                m.CreatedAt
            })
            .ToListAsync();

        return Ok(messages);
    }
}
