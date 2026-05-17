namespace server.Utils;

public static class TextSanitizer
{
    public static string SanitizeChat(string value)
    {
        return value
            .Trim()
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
