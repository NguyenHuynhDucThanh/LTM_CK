using System.Collections.Concurrent;
using server.Models;

namespace server.Managers;

public class RoomManager
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new();
    private readonly SessionManager _sessions;

    public RoomManager(SessionManager sessions)
    {
        _sessions = sessions;
    }

    public Room CreateRoom(PlayerSession host, string roomName, string questionSet)
    {
        LeaveRoom(host);
        host.Player.Score = 0;

        var room = new Room
        {
            Name = roomName.Trim(),
            HostId = host.Player.Id,
            QuestionSetName = questionSet,
            PlayerIds = [host.Player.Id]
        };

        host.CurrentRoomId = room.Id;
        _rooms[room.Id] = room;
        return room;
    }

    public Room? GetRoom(string roomId) => _rooms.TryGetValue(roomId, out var room) ? room : null;

    public List<Room> GetAllRooms() => _rooms.Values.OrderBy(r => r.Name).ToList();

    public bool JoinRoom(PlayerSession player, string roomId, out string error)
    {
        error = "";
        var room = GetRoom(roomId);
        if (room is null)
        {
            error = "Room không tồn tại.";
            return false;
        }

        if (room.GameSession.State != "WAITING")
        {
            error = "Game đã bắt đầu, không thể vào phòng.";
            return false;
        }

        if (player.CurrentRoomId is not null && player.CurrentRoomId != room.Id)
        {
            LeaveRoom(player);
        }

        if (!room.PlayerIds.Contains(player.Player.Id))
        {
            room.PlayerIds.Add(player.Player.Id);
        }

        player.Player.Score = 0;
        player.CurrentRoomId = room.Id;
        return true;
    }

    public Room? LeaveRoom(PlayerSession player)
    {
        if (player.CurrentRoomId is null) return null;
        var room = GetRoom(player.CurrentRoomId);
        if (room is null) return null;

        room.PlayerIds.Remove(player.Player.Id);
        player.CurrentRoomId = null;

        if (room.PlayerIds.Count == 0)
        {
            _rooms.TryRemove(room.Id, out _);
            return null;
        }

        if (room.HostId == player.Player.Id && room.GameSession.State == "WAITING")
        {
            room.HostId = room.PlayerIds[0];
        }

        return room;
    }

    public object ToDto(Room room)
    {
        return new
        {
            room.Id,
            room.Name,
            room.HostId,
            room.QuestionSetName,
            Status = room.GameSession.State,
            Players = room.PlayerIds
                .Select(id => _sessions.GetByPlayerId(id)?.Player)
                .Where(p => p is not null)
                .Select(p => new { p!.Id, p.Nickname, p.Score })
        };
    }
}
