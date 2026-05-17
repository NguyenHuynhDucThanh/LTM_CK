using System.Net.WebSockets;

namespace server.Models;

public class PlayerSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public Player Player { get; set; } = new();
    public WebSocket Socket { get; set; } = default!;
    public string? CurrentRoomId { get; set; }
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
}
