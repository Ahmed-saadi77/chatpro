using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    // Map userId to connectionId
    public static ConcurrentDictionary<string, string> userConnectionMap = new();

    // Your existing SendMessage method
    public async Task SendMessage(string senderId, string receiverId, string message, string? imageUrl)
    {
        var msg = new
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Text = message,
            ImageUrl = imageUrl,
            Timestamp = DateTime.UtcNow
        };

        if (userConnectionMap.TryGetValue(receiverId, out var receiverConnectionId))
        {
            await Clients.Client(receiverConnectionId).SendAsync("ReceiveMessage", msg);
        }

        if (userConnectionMap.TryGetValue(senderId, out var senderConnectionId))
        {
            await Clients.Client(senderConnectionId).SendAsync("ReceiveMessage", msg);
        }
    }


    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userId = httpContext.Request.Query["userId"].ToString();

        if (!string.IsNullOrEmpty(userId))
        {
            userConnectionMap[userId] = Context.ConnectionId;
            Console.WriteLine($"User connected: {userId} with connection {Context.ConnectionId}");
        }

        // Notify all clients about the updated online users list
        await Clients.All.SendAsync("GetOnlineUsers", userConnectionMap.Keys);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Find userId by connectionId and remove from map
        var user = userConnectionMap.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId);
        if (!string.IsNullOrEmpty(user.Key))
        {
            userConnectionMap.TryRemove(user.Key, out _);
            Console.WriteLine($"User disconnected: {user.Key} with connection {Context.ConnectionId}");
        }

        // Notify all clients about the updated online users list
        await Clients.All.SendAsync("GetOnlineUsers", userConnectionMap.Keys);

        await base.OnDisconnectedAsync(exception);
    }
}
