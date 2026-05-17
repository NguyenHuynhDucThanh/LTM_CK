using System.Collections.Concurrent;
using System.Net.WebSockets;
using server.Models;

namespace server.Managers;

public class SessionManager
{
    private readonly ConcurrentDictionary<string, PlayerSession> _sessions = new();

    public PlayerSession CreateSession(string nickname, WebSocket socket)
    {
        var session = new PlayerSession
        {
            Socket = socket,
            Player = new Player { Nickname = nickname.Trim() }
        };

        _sessions[session.SessionId] = session;
        return session;
    }

    public bool IsNicknameTaken(string nickname)
    {
        return _sessions.Values.Any(s =>
            s.Player.Nickname.Equals(nickname.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public PlayerSession? GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }

    public PlayerSession? GetByPlayerId(string playerId)
    {
        return _sessions.Values.FirstOrDefault(s => s.Player.Id == playerId);
    }

    public List<PlayerSession> GetAll() => _sessions.Values.ToList();

    public void Remove(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }
}
