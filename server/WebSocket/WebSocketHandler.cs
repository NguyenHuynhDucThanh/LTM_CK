using System.Net.WebSockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using server.Managers;
using server.Models;
using server.Protocol;
using server.Utils;

namespace server.WebSocketGateway;

public class WebSocketHandler
{
    private readonly SessionManager _sessions;
    private readonly RoomManager _rooms;
    private readonly QuestionManager _questions;
    private readonly GameManager _games;
    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public WebSocketHandler(SessionManager sessions, RoomManager rooms, QuestionManager questions, GameManager games)
    {
        _sessions = sessions;
        _rooms = rooms;
        _questions = questions;
        _games = games;
    }

    public async Task HandleConnectionAsync(WebSocket socket)
    {
        PlayerSession? session = null;
        var buffer = new byte[4096];

        try
        {
            Console.WriteLine($"[CONNECT] Một client mới đã kết nối.");
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var message = JsonSerializer.Deserialize<Message>(json, _json);
                if (message is null) continue;

                session = await HandleMessageAsync(socket, session, message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {session?.Player.Nickname ?? "Unknown"}: {ex.Message}");
        }
        finally
        {
            if (session is not null)
            {
                var nickname = session.Player.Nickname;
                var leftRoom = _rooms.LeaveRoom(session);
                _sessions.Remove(session.SessionId);
                if (leftRoom is not null)
                {
                    Console.WriteLine($"[ROOM] {nickname} đã rời phòng \"{leftRoom.Name}\".");
                    await BroadcastRoomAsync(leftRoom);
                }
                Console.WriteLine($"[DISCONNECT] {nickname} đã ngắt kết nối. Online: {_sessions.GetAll().Count}");
                await BroadcastLobbyAsync();
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                }
            }
        }
    }

    private async Task<PlayerSession?> HandleMessageAsync(WebSocket socket, PlayerSession? session, Message message)
    {
        switch (message.Type)
        {
            case MessageTypes.EnterNickname:
                return await EnterNicknameAsync(socket, message);
            case MessageTypes.CreateRoom when session is not null:
                await CreateRoomAsync(session, message);
                break;
            case MessageTypes.JoinRoom when session is not null:
                await JoinRoomAsync(session, message);
                break;
            case MessageTypes.LeaveRoom when session is not null:
                var previousRoom = _rooms.LeaveRoom(session);
                if (previousRoom is not null)
                {
                    await BroadcastRoomAsync(previousRoom);
                }
                await BroadcastLobbyAsync();
                break;
            case MessageTypes.Chat when session is not null:
                await ChatAsync(session, message);
                break;
            case MessageTypes.StartGame when session is not null:
                await _games.StartGameAsync(session, BroadcastRoomAsync, BroadcastLobbyAsync, BroadcastToRoomAsync);
                break;
            case MessageTypes.Answer when session is not null:
                await AnswerAsync(session, message);
                break;
            case MessageTypes.Heartbeat when session is not null:
                session.LastHeartbeat = DateTime.UtcNow;
                await SendAsync(session.Socket, MessageTypes.HeartbeatAck, new { });
                break;
        }

        return session;
    }

    private async Task<PlayerSession?> EnterNicknameAsync(WebSocket socket, Message message)
    {
        var nickname = message.Data.GetProperty("nickname").GetString()?.Trim() ?? "";
        if (!InputValidator.IsValidNickname(nickname))
        {
            await SendAsync(socket, MessageTypes.Error, new { message = "Nickname phai tu 2 den 20 ky tu." });
            return null;
        }

        if (_sessions.IsNicknameTaken(nickname))
        {
            await SendAsync(socket, MessageTypes.Error, new { message = "Nickname da duoc su dung." });
            return null;
        }

        var session = _sessions.CreateSession(nickname, socket);
        Console.WriteLine($"[JOIN] {nickname} đã tham gia hệ thống. Online: {_sessions.GetAll().Count}");
        await SendAsync(socket, MessageTypes.SessionCreated, new
        {
            sessionId = session.SessionId,
            player = new { session.Player.Id, session.Player.Nickname, session.Player.Score }
        });
        await BroadcastLobbyAsync();
        return session;
    }

    private async Task CreateRoomAsync(PlayerSession session, Message message)
    {
        var roomName = message.Data.GetProperty("roomName").GetString() ?? "";
        var questionSet = message.Data.GetProperty("questionSet").GetString() ?? "";

        if (!InputValidator.IsValidRoomName(roomName) || _questions.GetQuestions(questionSet).Count == 0)
        {
            await SendAsync(session.Socket, MessageTypes.Error, new { message = "Ten phong hoac bo cau hoi khong hop le." });
            return;
        }

        var previousRoom = _rooms.LeaveRoom(session);
        if (previousRoom is not null)
        {
            await BroadcastRoomAsync(previousRoom);
        }

        var room = _rooms.CreateRoom(session, roomName, questionSet);
        Console.WriteLine($"[ROOM] {session.Player.Nickname} đã tạo phòng \"{roomName}\" (Question set: {questionSet}).");
        await BroadcastLobbyAsync();
        await BroadcastRoomAsync(room);
    }

    private async Task JoinRoomAsync(PlayerSession session, Message message)
    {
        var roomId = message.Data.GetProperty("roomId").GetString() ?? "";
        var previousRoom = session.CurrentRoomId is null ? null : _rooms.GetRoom(session.CurrentRoomId);
        if (!_rooms.JoinRoom(session, roomId, out var error))
        {
            await SendAsync(session.Socket, MessageTypes.Error, new { message = error });
            return;
        }

        var room = _rooms.GetRoom(roomId);
        if (room is not null)
        {
            if (previousRoom is not null && previousRoom.Id != room.Id)
            {
                await BroadcastRoomAsync(previousRoom);
            }
            await BroadcastLobbyAsync();
            Console.WriteLine($"[ROOM] {session.Player.Nickname} đã vào phòng \"{room.Name}\".");
            await BroadcastRoomAsync(room);
        }
    }

    private async Task ChatAsync(PlayerSession session, Message message)
    {
        var text = message.Data.GetProperty("text").GetString()?.Trim() ?? "";
        if (!InputValidator.IsValidChatMessage(text)) return;
        text = TextSanitizer.SanitizeChat(text);

        var room = session.CurrentRoomId is null ? null : _rooms.GetRoom(session.CurrentRoomId);
        if (room is null) return;

        await BroadcastToRoomAsync(room, MessageTypes.Chat, new
        {
            player = session.Player.Nickname,
            text,
            time = DateTime.Now.ToString("HH:mm:ss")
        });
        Console.WriteLine($"[CHAT] [{room.Name}] {session.Player.Nickname}: {text}");
    }

    private async Task AnswerAsync(PlayerSession session, Message message)
    {
        var answer = message.Data.GetProperty("answer").GetString() ?? "";
        await _games.SubmitAnswerAsync(session, answer, BroadcastToRoomAsync);
    }

    private object GetLobbyDto()
    {
        return new
        {
            rooms = _rooms.GetAllRooms().Select(_rooms.ToDto),
            players = _sessions.GetAll().Select(s => new { s.Player.Id, s.Player.Nickname }),
            questionSets = _questions.GetSetNames()
        };
    }

    private async Task BroadcastLobbyAsync()
    {
        foreach (var session in _sessions.GetAll())
        {
            await SendAsync(session.Socket, MessageTypes.LobbyUpdated, GetLobbyDto());
        }
    }

    private async Task BroadcastRoomAsync(Room room)
    {
        await BroadcastToRoomAsync(room, MessageTypes.RoomUpdated, _rooms.ToDto(room));
    }

    private async Task BroadcastToRoomAsync(Room room, string type, object data)
    {
        foreach (var playerId in room.PlayerIds)
        {
            var session = _sessions.GetByPlayerId(playerId);
            if (session is not null)
            {
                await SendAsync(session.Socket, type, data);
            }
        }
    }

    private async Task SendAsync(WebSocket socket, string type, object data)
    {
        if (socket.State != WebSocketState.Open) return;
        var payload = JsonSerializer.Serialize(new
        {
            type,
            data,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            requestId = Guid.NewGuid().ToString()
        }, _json);

        var bytes = Encoding.UTF8.GetBytes(payload);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
