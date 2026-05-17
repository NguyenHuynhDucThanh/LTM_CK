namespace server.Models;

public class Room
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string HostId { get; set; } = "";
    public string QuestionSetName { get; set; } = "";
    public List<string> PlayerIds { get; set; } = [];
    public GameSession GameSession { get; set; } = new();
}
