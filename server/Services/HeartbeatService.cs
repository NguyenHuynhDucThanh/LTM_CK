using System.Net.WebSockets;
using server.Managers;

namespace server.Services;

public class HeartbeatService : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
    private readonly SessionManager _sessions;

    public HeartbeatService(SessionManager sessions)
    {
        _sessions = sessions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_checkInterval, stoppingToken);

            foreach (var session in _sessions.GetAll())
            {
                if (DateTime.UtcNow - session.LastHeartbeat > _timeout)
                {
                    try
                    {
                        if (session.Socket.State == WebSocketState.Open)
                        {
                            Console.WriteLine($"[HEARTBEAT] {session.Player.Nickname} timeout ({_timeout.TotalSeconds}s) - đóng kết nối.");
                            await session.Socket.CloseAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "Heartbeat timeout",
                                CancellationToken.None);
                        }
                    }
                    catch { /* Socket may already be disposed */ }
                }
            }
        }
    }
}
