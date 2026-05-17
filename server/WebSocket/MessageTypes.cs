namespace server.Protocol;

public static class MessageTypes
{
    public const string EnterNickname = "ENTER_NICKNAME";
    public const string SessionCreated = "SESSION_CREATED";
    public const string LobbyUpdated = "LOBBY_UPDATED";
    public const string CreateRoom = "CREATE_ROOM";
    public const string JoinRoom = "JOIN_ROOM";
    public const string LeaveRoom = "LEAVE_ROOM";
    public const string RoomUpdated = "ROOM_UPDATED";
    public const string StartGame = "START_GAME";
    public const string Question = "QUESTION";
    public const string Answer = "ANSWER";
    public const string Result = "RESULT";
    public const string Scoreboard = "SCOREBOARD";
    public const string GameOver = "GAME_OVER";
    public const string Chat = "CHAT";
    public const string Error = "ERROR";
    public const string Heartbeat = "HEARTBEAT";
    public const string HeartbeatAck = "HEARTBEAT_ACK";
}
