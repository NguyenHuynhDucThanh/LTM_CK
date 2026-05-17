using System.Text.Json;

namespace server.Models;

public class Message
{
    public string Type { get; set; } = "";
    public JsonElement Data { get; set; }
    public long Timestamp { get; set; }
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
}
