using server.Models;
using server.Protocol;
using server.Services;

namespace server.Managers;

public class GameManager
{
    private const int QuestionTimeLimitSeconds = 20;
    private const int ResultDelaySeconds = 4;
    private const int MaxQuestionsPerGame = 10;
    private readonly SessionManager _sessions;
    private readonly RoomManager _rooms;
    private readonly QuestionManager _questions;
    private readonly ScoreService _scores;

    public GameManager(SessionManager sessions, RoomManager rooms, QuestionManager questions, ScoreService scores)
    {
        _sessions = sessions;
        _rooms = rooms;
        _questions = questions;
        _scores = scores;
    }

    public async Task StartGameAsync(
        PlayerSession host,
        Func<Room, Task> broadcastRoomAsync,
        Func<Task> broadcastLobbyAsync,
        Func<Room, string, object, Task> broadcastToRoomAsync)
    {
        var room = host.CurrentRoomId is null ? null : _rooms.GetRoom(host.CurrentRoomId);
        if (room is null || room.HostId != host.Player.Id || room.GameSession.State != "WAITING") return;

        room.GameSession.State = "PLAYING";
        room.GameSession.CurrentIndex = -1;
        room.GameSession.CurrentQuestion = null;
        room.GameSession.TimeLimitSeconds = QuestionTimeLimitSeconds;
        room.GameSession.Answers.Clear();

        foreach (var playerId in room.PlayerIds)
        {
            var player = _sessions.GetByPlayerId(playerId)?.Player;
            if (player is not null) player.Score = 0;
        }

        await broadcastRoomAsync(room);
        await broadcastLobbyAsync();
        Console.WriteLine($"[GAME] Phòng \"{room.Name}\" bắt đầu game! ({room.PlayerIds.Count} người chơi)");
        _ = RunGameAsync(room, broadcastRoomAsync, broadcastLobbyAsync, broadcastToRoomAsync);
    }

    public async Task SubmitAnswerAsync(
        PlayerSession session,
        string answer,
        Func<Room, string, object, Task> broadcastToRoomAsync)
    {
        var room = session.CurrentRoomId is null ? null : _rooms.GetRoom(session.CurrentRoomId);
        if (room is null || room.GameSession.State != "PLAYING" || room.GameSession.Answers.ContainsKey(session.Player.Id)) return;

        room.GameSession.Answers[session.Player.Id] = answer;

        var elapsed = Math.Max(0, (DateTime.UtcNow - room.GameSession.QuestionStartedAt).TotalSeconds);
        var current = room.GameSession.CurrentQuestion;
        var isCorrect = current is not null && answer.Equals(current.Correct, StringComparison.OrdinalIgnoreCase);
        session.Player.Score += _scores.Calculate(isCorrect, elapsed);
        Console.WriteLine($"[GAME] {session.Player.Nickname} trả lời {answer} - {(isCorrect ? "Đúng" : "Sai")} ({elapsed:F1}s) - Tổng điểm: {session.Player.Score}");

        await broadcastToRoomAsync(room, MessageTypes.Scoreboard, new { scoreboard = GetScoreboard(room) });
    }

    private async Task RunGameAsync(
        Room room,
        Func<Room, Task> broadcastRoomAsync,
        Func<Task> broadcastLobbyAsync,
        Func<Room, string, object, Task> broadcastToRoomAsync)
    {
        var list = _questions.GetQuestions(room.QuestionSetName).Take(MaxQuestionsPerGame).ToList();
        for (var i = 0; i < list.Count; i++)
        {
            if (room.GameSession.State != "PLAYING") return;

            var question = list[i];
            room.GameSession.CurrentIndex = i;
            room.GameSession.CurrentQuestion = question;
            room.GameSession.Answers.Clear();
            room.GameSession.QuestionStartedAt = DateTime.UtcNow;

            Console.WriteLine($"[GAME] [{room.Name}] Câu {i + 1}/{list.Count}: {question.Content}");
            await broadcastToRoomAsync(room, MessageTypes.Question, new
            {
                index = i + 1,
                total = list.Count,
                timeLimit = room.GameSession.TimeLimitSeconds,
                question = new { question.Content, question.A, question.B, question.C, question.D }
            });

            await WaitForAnswersOrTimeoutAsync(room, TimeSpan.FromSeconds(room.GameSession.TimeLimitSeconds));

            await broadcastToRoomAsync(room, MessageTypes.Result, new
            {
                correct = question.Correct,
                scoreboard = GetScoreboard(room)
            });

            await Task.Delay(TimeSpan.FromSeconds(ResultDelaySeconds));
        }

        room.GameSession.State = "WAITING";
        room.GameSession.CurrentIndex = -1;
        room.GameSession.CurrentQuestion = null;
        room.GameSession.Answers.Clear();
        Console.WriteLine($"[GAME] Phòng \"{room.Name}\" kết thúc game!");
        await broadcastToRoomAsync(room, MessageTypes.GameOver, new { scoreboard = GetScoreboard(room) });
        await broadcastRoomAsync(room);
        await broadcastLobbyAsync();
    }

    private static async Task WaitForAnswersOrTimeoutAsync(Room room, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (room.PlayerIds.Count > 0 && room.GameSession.Answers.Count >= room.PlayerIds.Count)
            {
                return;
            }

            await Task.Delay(200);
        }
    }

    public object GetScoreboard(Room room)
    {
        return room.PlayerIds
            .Select(id => _sessions.GetByPlayerId(id)?.Player)
            .Where(p => p is not null)
            .OrderByDescending(p => p!.Score)
            .Select(p => new { p!.Nickname, p.Score });
    }
}
