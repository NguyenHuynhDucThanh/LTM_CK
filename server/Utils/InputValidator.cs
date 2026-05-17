namespace server.Utils;

public static class InputValidator
{
    public static bool IsValidNickname(string nickname)
    {
        return !string.IsNullOrWhiteSpace(nickname)
            && nickname.Trim().Length >= 2
            && nickname.Trim().Length <= 20;
    }

    public static bool IsValidRoomName(string roomName)
    {
        return !string.IsNullOrWhiteSpace(roomName)
            && roomName.Trim().Length >= 2
            && roomName.Trim().Length <= 40;
    }

    public static bool IsValidChatMessage(string message)
    {
        return !string.IsNullOrWhiteSpace(message)
            && message.Trim().Length <= 200;
    }
}
